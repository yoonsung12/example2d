using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance    { get; private set; }
    /// <summary>씬 전환 시 SceneTransition → SpawnPoint 로 전달되는 스폰 ID.</summary>
    public static string      PendingSpawnId { get; set; }

    public enum GameState { Playing, Paused, GameOver }
    public GameState CurrentState { get; private set; } = GameState.Playing;

    public CharacterBase Player { get; private set; }

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()  => UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene _, UnityEngine.SceneManagement.LoadSceneMode __)
        => FindPlayer();

    private void Start() => FindPlayer();

    public void FindPlayer()
    {
        var pc = FindFirstObjectByType<PlayerController>();
        if (pc != null) Player = pc.GetComponent<CharacterBase>();
    }

    public void PauseGame()
    {
        CurrentState = GameState.Paused;
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        CurrentState = GameState.Playing;
        Time.timeScale = 1f;
    }

    public void GameOver()
    {
        CurrentState = GameState.GameOver;
        Time.timeScale = 0f;
    }

    public void RestartScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
