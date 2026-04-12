"""
FCM 기반 게임 플레이 스타일 클러스터링 분석
RBFN_FCM 문서 기반 구현
- 3개 클러스터: 회피형 / 중립형 / 공격형
- 입력: Combat_Engagement_Rate, Skip_Rate, HP_Threshold_Retreat, Enemy_Clear_Rate
- 출력: 팀별 소속도 비율 (산점도 + 누적 막대그래프)
"""

from __future__ import annotations

import os
from dataclasses import dataclass, field
from pathlib import Path
from typing import Protocol

import numpy as np
import pandas as pd
import matplotlib
import matplotlib.pyplot as plt
import matplotlib.ticker as mticker

matplotlib.rcParams["font.family"] = "Malgun Gothic"
matplotlib.rcParams["axes.unicode_minus"] = False

# ─────────────────────────────────────────────
# 상수
# ─────────────────────────────────────────────
FEATURES = [
    "Combat_Engagement_Rate",
    "Skip_Rate",
    "HP_Threshold_Retreat",
    "Enemy_Clear_Rate",
]
CLUSTER_NAMES = ["회피형", "중립형", "공격형"]
CLUSTER_COLORS = ["#4C72B0", "#55A868", "#C44E52"]   # 파랑·초록·빨강

N_CLUSTERS   = 3
FUZZIFIER    = 2.0    # m
MAX_ITER     = 500
EPSILON      = 1e-5
RANDOM_SEED  = 42


# ─────────────────────────────────────────────
# 1. 데이터 로더  (SRP)
# ─────────────────────────────────────────────
class GameDataLoader:
    """CSV 로드 및 [0,1] 클리핑 정규화."""

    def __init__(self, csv_path: str | Path) -> None:
        self._path = Path(csv_path)

    def load(self) -> tuple[np.ndarray, np.ndarray]:
        """
        Returns
        -------
        X      : (N, 4) float64, 정규화된 피처 행렬
        team_ids: (N,) str 배열
        """
        df = pd.read_csv(self._path)
        missing = [c for c in ["team_id"] + FEATURES if c not in df.columns]
        if missing:
            raise ValueError(f"CSV에 필수 컬럼이 없습니다: {missing}")

        team_ids = df["team_id"].to_numpy(dtype=str)
        X = df[FEATURES].to_numpy(dtype=np.float64)
        X = np.clip(X, 0.0, 1.0)   # 문서 2-1절: 모든 피처 [0,1] 정규화
        return X, team_ids


# ─────────────────────────────────────────────
# 2. FCM 알고리즘  (SRP · OCP)
# ─────────────────────────────────────────────
@dataclass
class FCMResult:
    U: np.ndarray           # (N, C) 소속도 행렬
    centroids: np.ndarray   # (C, F) 센트로이드
    n_iter: int
    converged: bool


class FuzzyCMeans:
    """
    Fuzzy C-Means 클러스터링.
    수식: U_ij = 1 / Σ_k (d_ij / d_ik)^(2/(m-1))
    """

    def __init__(
        self,
        n_clusters: int = N_CLUSTERS,
        fuzzifier: float = FUZZIFIER,
        max_iter: int = MAX_ITER,
        epsilon: float = EPSILON,
        random_seed: int = RANDOM_SEED,
    ) -> None:
        self.c = n_clusters
        self.m = fuzzifier
        self.max_iter = max_iter
        self.eps = epsilon
        self.rng = np.random.default_rng(random_seed)

    # ── 공개 인터페이스 ──────────────────────────
    def fit(self, X: np.ndarray) -> FCMResult:
        U = self._init_membership(X.shape[0])
        centroids = np.zeros((self.c, X.shape[1]))
        converged = False

        for iteration in range(self.max_iter):
            centroids = self._update_centroids(X, U)
            U_new = self._update_membership(X, centroids)

            delta = float(np.linalg.norm(U_new - U))
            U = U_new
            if delta < self.eps:
                converged = True
                break

        return FCMResult(U=U, centroids=centroids, n_iter=iteration + 1, converged=converged)

    # ── 내부 구현 ────────────────────────────────
    def _init_membership(self, n: int) -> np.ndarray:
        """행합이 1이 되도록 랜덤 초기화."""
        U = self.rng.dirichlet(alpha=np.ones(self.c), size=n)
        return U  # (N, C)

    def _update_centroids(self, X: np.ndarray, U: np.ndarray) -> np.ndarray:
        """V_i = Σ u_ij^m * x_j / Σ u_ij^m"""
        Um = U ** self.m          # (N, C)
        centroids = (Um.T @ X) / Um.sum(axis=0, keepdims=True).T
        return centroids          # (C, F)

    def _update_membership(self, X: np.ndarray, centroids: np.ndarray) -> np.ndarray:
        """U_ij = 1 / Σ_k (d_ij/d_ik)^(2/(m-1))"""
        exp = 2.0 / (self.m - 1.0)
        N = X.shape[0]
        dist = np.zeros((N, self.c))   # (N, C)

        for k in range(self.c):
            diff = X - centroids[k]    # (N, F)
            dist[:, k] = np.sqrt((diff ** 2).sum(axis=1)) + 1e-10

        U_new = np.zeros_like(dist)
        for i in range(self.c):
            ratio = dist[:, i : i + 1] / dist        # (N, C)
            U_new[:, i] = 1.0 / (ratio ** exp).sum(axis=1)

        return U_new  # (N, C)


# ─────────────────────────────────────────────
# 3. 클러스터 레이블러  (Factory Method)
# ─────────────────────────────────────────────
class ClusterLabeler:
    """
    센트로이드 분석으로 클러스터 인덱스를 문서 정의 레이블에 매핑.
    회피형: Skip↑ & Engage↓  →  PlayStyle_Score ≈ 0
    공격형: Engage↑ & Clear↑ →  PlayStyle_Score ≈ 1
    """

    # 피처 인덱스 (FEATURES 순서)
    _IDX_ENGAGE = 0   # Combat_Engagement_Rate
    _IDX_SKIP   = 1   # Skip_Rate
    _IDX_CLEAR  = 3   # Enemy_Clear_Rate

    def label(self, centroids: np.ndarray) -> list[str]:
        """
        Returns
        -------
        label_map : 클러스터 인덱스 → 레이블명 리스트
        """
        play_scores = self._compute_play_scores(centroids)
        order = np.argsort(play_scores)   # 낮은 순 (회피형 → 중립 → 공격형)

        label_map = [""] * N_CLUSTERS
        for rank, cluster_idx in enumerate(order):
            label_map[cluster_idx] = CLUSTER_NAMES[rank]
        return label_map

    def _compute_play_scores(self, centroids: np.ndarray) -> np.ndarray:
        """문서 2-2절: PlayStyle_Score = 0.5*Engage + 0.3*Clear - 0.2*Skip"""
        engage = centroids[:, self._IDX_ENGAGE]
        skip   = centroids[:, self._IDX_SKIP]
        clear  = centroids[:, self._IDX_CLEAR]
        return 0.5 * engage + 0.3 * clear - 0.2 * skip


# ─────────────────────────────────────────────
# 4. 팀 비율 계산기  (SRP)
# ─────────────────────────────────────────────
@dataclass
class TeamRatio:
    team_id: str
    ratios: dict[str, float]   # {레이블: 소속도 비율}
    dominant: str              # 주 클러스터


class TeamRatioCalculator:
    """
    팀별 평균 소속도 벡터 → 비율 계산.
    Hard label 대신 Fuzzy 소속도를 그대로 집계.
    """

    def calculate(
        self,
        X: np.ndarray,
        team_ids: np.ndarray,
        U: np.ndarray,
        label_map: list[str],
    ) -> list[TeamRatio]:
        results: list[TeamRatio] = []
        for tid in np.unique(team_ids):
            mask = team_ids == tid
            avg_u = U[mask].mean(axis=0)   # (C,) 평균 소속도
            # 소속도 합이 1이 되도록 재정규화
            avg_u /= avg_u.sum()

            ratios = {label_map[k]: float(avg_u[k]) for k in range(N_CLUSTERS)}
            dominant = max(ratios, key=lambda lbl: ratios[lbl])
            results.append(TeamRatio(team_id=tid, ratios=ratios, dominant=dominant))

        return sorted(results, key=lambda r: r.team_id)


# ─────────────────────────────────────────────
# 5. 시각화  (Template Method)
# ─────────────────────────────────────────────
class Visualizer:
    """산점도 + 누적 막대그래프를 생성하고 저장."""

    def __init__(self, output_dir: str | Path) -> None:
        self._out = Path(output_dir)
        self._out.mkdir(parents=True, exist_ok=True)

    # ── 공개 인터페이스 ──────────────────────────
    def draw_scatter(
        self,
        X: np.ndarray,
        U: np.ndarray,
        label_map: list[str],
        centroids: np.ndarray,
    ) -> Path:
        """Combat_Engagement_Rate vs Enemy_Clear_Rate 산점도."""
        fig, ax = plt.subplots(figsize=(8, 6))
        hard_labels = np.argmax(U, axis=1)

        for k, (name, color) in enumerate(zip(CLUSTER_NAMES, CLUSTER_COLORS)):
            mask = hard_labels == self._cluster_index(label_map, name)
            if mask.any():
                ax.scatter(
                    X[mask, 0], X[mask, 3],
                    c=color, label=name, alpha=0.75, s=60, edgecolors="white", linewidths=0.5,
                )

        # 센트로이드
        for k, (name, color) in enumerate(zip(CLUSTER_NAMES, CLUSTER_COLORS)):
            ci = self._cluster_index(label_map, name)
            ax.scatter(
                centroids[ci, 0], centroids[ci, 3],
                c=color, marker="X", s=200, edgecolors="black", linewidths=1.2,
                zorder=5,
            )
            ax.annotate(
                f"{name}\n센트로이드",
                xy=(centroids[ci, 0], centroids[ci, 3]),
                xytext=(8, 8), textcoords="offset points",
                fontsize=8, color=color,
            )

        ax.set_xlabel("Combat Engagement Rate (전투 참여율)", fontsize=11)
        ax.set_ylabel("Enemy Clear Rate (적 처치율)", fontsize=11)
        ax.set_title("FCM 클러스터링 — 플레이 스타일 산점도", fontsize=13, fontweight="bold")
        ax.legend(fontsize=10)
        ax.grid(True, alpha=0.3)
        ax.set_xlim(-0.05, 1.05)
        ax.set_ylim(-0.05, 1.05)

        path = self._out / "scatter_plot.png"
        fig.tight_layout()
        fig.savefig(path, dpi=150)
        plt.close(fig)
        return path

    def draw_bar(self, team_ratios: list[TeamRatio]) -> Path:
        """팀별 클러스터 소속 비율 누적 막대그래프."""
        team_ids = [tr.team_id for tr in team_ratios]
        data = {name: [tr.ratios[name] for tr in team_ratios] for name in CLUSTER_NAMES}

        fig, ax = plt.subplots(figsize=(10, 5))
        bottoms = np.zeros(len(team_ids))

        for name, color in zip(CLUSTER_NAMES, CLUSTER_COLORS):
            vals = np.array(data[name])
            bars = ax.bar(team_ids, vals, bottom=bottoms, color=color, label=name, width=0.6)
            # 비율 텍스트 (5% 이상일 때만)
            for bar, v in zip(bars, vals):
                if v >= 0.05:
                    ax.text(
                        bar.get_x() + bar.get_width() / 2,
                        bar.get_y() + bar.get_height() / 2,
                        f"{v:.0%}",
                        ha="center", va="center", fontsize=8, color="white", fontweight="bold",
                    )
            bottoms += vals

        ax.set_xlabel("팀 ID", fontsize=11)
        ax.set_ylabel("소속도 비율", fontsize=11)
        ax.set_title("팀별 플레이 스타일 소속 비율 (FCM)", fontsize=13, fontweight="bold")
        ax.yaxis.set_major_formatter(mticker.PercentFormatter(xmax=1.0))
        ax.legend(loc="upper right", fontsize=10)
        ax.set_ylim(0, 1.05)
        ax.grid(axis="y", alpha=0.3)

        path = self._out / "bar_chart.png"
        fig.tight_layout()
        fig.savefig(path, dpi=150)
        plt.close(fig)
        return path

    # ── 헬퍼 ─────────────────────────────────────
    @staticmethod
    def _cluster_index(label_map: list[str], name: str) -> int:
        return label_map.index(name)


# ─────────────────────────────────────────────
# 6. 파이프라인  (Facade)
# ─────────────────────────────────────────────
class FCMAnalysisPipeline:
    """전체 분석 흐름 조율 — 외부에서는 이 클래스만 사용."""

    def __init__(
        self,
        csv_path: str | Path,
        output_dir: str | Path,
    ) -> None:
        self._loader     = GameDataLoader(csv_path)
        self._fcm        = FuzzyCMeans()
        self._labeler    = ClusterLabeler()
        self._calculator = TeamRatioCalculator()
        self._visualizer = Visualizer(output_dir)

    def run(self) -> None:
        # 1) 데이터 로드
        print("[1/5] 데이터 로드 중...")
        X, team_ids = self._loader.load()
        print(f"      → {len(X)}개 샘플, {len(np.unique(team_ids))}개 팀")

        # 2) FCM 클러스터링
        print("[2/5] FCM 클러스터링 실행 중...")
        result = self._fcm.fit(X)
        status = "수렴" if result.converged else f"미수렴({result.n_iter}회)"
        print(f"      → {result.n_iter}번 반복 후 {status}")

        # 3) 레이블 매핑
        print("[3/5] 클러스터 레이블 매핑 중...")
        label_map = self._labeler.label(result.centroids)
        self._print_centroids(result.centroids, label_map)

        # 4) 팀별 비율 계산
        print("[4/5] 팀별 소속 비율 계산 중...")
        team_ratios = self._calculator.calculate(X, team_ids, result.U, label_map)
        self._print_team_ratios(team_ratios)

        # 5) 시각화
        print("[5/5] 시각화 저장 중...")
        scatter_path = self._visualizer.draw_scatter(X, result.U, label_map, result.centroids)
        bar_path     = self._visualizer.draw_bar(team_ratios)
        print(f"      → 산점도  : {scatter_path}")
        print(f"      → 막대그래프: {bar_path}")
        print("\n분석 완료.")

    # ── 출력 헬퍼 ─────────────────────────────────
    @staticmethod
    def _print_centroids(centroids: np.ndarray, label_map: list[str]) -> None:
        print("\n  [클러스터 센트로이드]")
        header = f"  {'클러스터':<8}" + "".join(f"  {f[:12]:>14}" for f in FEATURES)
        print(header)
        print("  " + "-" * (8 + 16 * len(FEATURES)))
        for k, name in enumerate(label_map):
            row = f"  {name:<8}" + "".join(f"  {centroids[k, j]:>14.4f}" for j in range(len(FEATURES)))
            print(row)
        print()

    @staticmethod
    def _print_team_ratios(team_ratios: list[TeamRatio]) -> None:
        print("\n  [팀별 소속 비율]")
        header = f"  {'팀':>5}  {'회피형':>8}  {'중립형':>8}  {'공격형':>8}  {'주 스타일'}"
        print(header)
        print("  " + "-" * 50)
        for tr in team_ratios:
            row = (
                f"  {tr.team_id:>5}"
                f"  {tr.ratios['회피형']:>7.1%}"
                f"  {tr.ratios['중립형']:>7.1%}"
                f"  {tr.ratios['공격형']:>7.1%}"
                f"  {tr.dominant}"
            )
            print(row)
        print()


# ─────────────────────────────────────────────
# 진입점
# ─────────────────────────────────────────────
if __name__ == "__main__":
    BASE_DIR = Path(__file__).parent
    pipeline = FCMAnalysisPipeline(
        csv_path   = BASE_DIR / "game_data.csv",
        output_dir = BASE_DIR / "results",
    )
    pipeline.run()
