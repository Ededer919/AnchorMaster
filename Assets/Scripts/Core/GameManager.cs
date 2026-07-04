using UnityEngine;

/// <summary>
/// 游戏状态枚举
/// </summary>
public enum GameState
{
    Menu,
    Playing,
    Paused,
    GameOver
}

/// <summary>
/// 游戏管理器 - 控制游戏流程和状态
/// </summary>
public class GameManager : Singleton<GameManager>
{
    [Header("Game Settings")]
    public int gridSize = 4;

    [Header("Level Settings")]
    public int currentLevel = 1;

    public GameState CurrentState { get; private set; } = GameState.Menu;

    private void Start()
    {
        InitializeGame();
    }

    /// <summary>
    /// 初始化游戏
    /// </summary>
    public void InitializeGame()
    {
        ChangeState(GameState.Playing);
        Debug.Log("Game Initialized - Level: " + currentLevel);

        EventSystem.Instance.Publish(new GameStartEvent());
        UIManager.Instance?.UpdateLevel(currentLevel);
    }

    /// <summary>
    /// 切换游戏状态
    /// </summary>
    public void ChangeState(GameState newState)
    {
        if (CurrentState == newState) return;

        GameState oldState = CurrentState;
        CurrentState = newState;

        EventSystem.Instance.Publish(new GameStateChangedEvent
        {
            NewState = newState,
            OldState = oldState
        });

        Debug.Log($"Game State: {oldState} -> {newState}");
    }

    /// <summary>
    /// 暂停游戏
    /// </summary>
    public void PauseGame()
    {
        if (CurrentState != GameState.Playing) return;

        ChangeState(GameState.Paused);
        Time.timeScale = 0f;
        EventSystem.Instance.Publish(new GamePauseEvent { IsPaused = true });
    }

    /// <summary>
    /// 继续游戏
    /// </summary>
    public void ResumeGame()
    {
        if (CurrentState != GameState.Paused) return;

        ChangeState(GameState.Playing);
        Time.timeScale = 1f;
        EventSystem.Instance.Publish(new GamePauseEvent { IsPaused = false });
    }

    /// <summary>
    /// 开始灾难
    /// </summary>
    public void StartDisaster()
    {
        if (CurrentState != GameState.Playing) return;

        UIManager.Instance?.UpdateStatus("Disaster incoming!");
        FindFirstObjectByType<DisasterWindController>()?.StartDisasterWind();
        StartCoroutine(DisasterCoroutine());
    }

    private System.Collections.IEnumerator DisasterCoroutine()
    {
        yield return new WaitForSeconds(10f);

        if (CurrentState == GameState.Playing)
        {
            LevelComplete();
        }
    }

    /// <summary>
    /// 关卡完成
    /// </summary>
    public void LevelComplete()
    {
        Debug.Log("Level " + currentLevel + " completed!");
        ChangeState(GameState.GameOver);

        EventSystem.Instance.Publish(new GameOverEvent { IsWin = true, FinalScore = 0 });
    }

    /// <summary>
    /// 关卡失败
    /// </summary>
    public void LevelFail()
    {
        Debug.Log("Level " + currentLevel + " failed!");
        ChangeState(GameState.GameOver);

        EventSystem.Instance.Publish(new GameOverEvent { IsWin = false, FinalScore = 0 });
    }

    /// <summary>
    /// 重新开始游戏
    /// </summary>
    public void RestartGame()
    {
        currentLevel = 1;
        Time.timeScale = 1f;
        InitializeGame();
    }

    /// <summary>
    /// 下一关
    /// </summary>
    public void NextLevel()
    {
        currentLevel++;
        InitializeGame();
    }

    /// <summary>
    /// 拼图完成
    /// </summary>
    public void OnPuzzleSolved()
    {
        Debug.Log("Puzzle solved!");
        LevelComplete();
    }
}
