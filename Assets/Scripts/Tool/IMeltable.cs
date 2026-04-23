/// <summary>라이터로 녹일 수 있는 오브젝트(얼음·빙판 등)가 구현하는 인터페이스</summary>
public interface IMeltable
{
    /// <summary>녹음 처리 (지형 변형, 오브젝트 제거 등)</summary>
    void Melt();
}
