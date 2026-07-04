using UnityEngine;

/// <summary>
/// 游戏启动器 - 初始化所有框架组件
/// 挂载到场景中的空 GameObject 上
/// </summary>
public class GameLaunch : MonoBehaviour
{
    [Header("Manager Settings")]
    public bool createGameManager = true;
    public bool createUIManager = true;
    public bool createInputManager = true;
    public bool createTimerManager = true;
    public bool createResourceManager = true;

    [Header("Game Settings")]
    public int gridSize = 4;
    public int startLevel = 1;

    private void Awake()
    {
        Debug.Log("[GameLaunch] 开始初始化游戏框架...");
    }

    private void Start()
    {
        InitializeManagers();
        InitializeGame();

        Debug.Log("[GameLaunch] 游戏框架初始化完成!");
    }

    /// <summary>
    /// 初始化所有管理器
    /// </summary>
    private void InitializeManagers()
    {
        // 确保 GameManager 存在
        if (createGameManager)
        {
            EnsureManager<GameManager>();
        }

        // 确保 UIManager 存在
        if (createUIManager)
        {
            EnsureManager<UIManager>();
        }

        // 确保 InputManager 存在
        if (createInputManager)
        {
            EnsureManager<InputManager>();
        }

        // 确保 TimerManager 存在
        if (createTimerManager)
        {
            EnsureManager<TimerManager>();
        }

        // 确保 ResourceManager 存在
        if (createResourceManager)
        {
            EnsureManager<ResourceManager>();
        }
    }

    /// <summary>
    /// 确保管理器存在，如果不存在则创建
    /// </summary>
    private T EnsureManager<T>() where T : MonoBehaviour
    {
        T manager = FindObjectOfType<T>();

        if (manager == null)
        {
            GameObject managerObj = new GameObject(typeof(T).Name);
            manager = managerObj.AddComponent<T>();
            Debug.Log($"[GameLaunch] 创建了 {typeof(T).Name}");
        }
        else
        {
            Debug.Log($"[GameLaunch] {typeof(T).Name} 已存在");
        }

        return manager;
    }

    /// <summary>
    /// 初始化游戏设置
    /// </summary>
    private void InitializeGame()
    {
        // 设置游戏参数
        if (GameManager.Instance != null)
        {
            GameManager.Instance.gridSize = gridSize;
            GameManager.Instance.currentLevel = startLevel;
        }

        // 订阅事件
        SubscribeToEvents();
    }

    /// <summary>
    /// 订阅游戏事件
    /// </summary>
    private void SubscribeToEvents()
    {
        EventSystem.Instance.Subscribe<GameStartEvent>(OnGameStart);
        EventSystem.Instance.Subscribe<GameOverEvent>(OnGameOver);
        EventSystem.Instance.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
    }

    /// <summary>
    /// 取消订阅事件
    /// </summary>
    private void UnsubscribeFromEvents()
    {
        EventSystem.Instance.Unsubscribe<GameStartEvent>(OnGameStart);
        EventSystem.Instance.Unsubscribe<GameOverEvent>(OnGameOver);
        EventSystem.Instance.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    // ========== 事件处理 ==========

    private void OnGameStart(GameStartEvent evt)
    {
        Debug.Log("[GameLaunch] 游戏开始");
    }

    private void OnGameOver(GameOverEvent evt)
    {
        Debug.Log($"[GameLaunch] 游戏结束 - 胜利: {evt.IsWin}, 分数: {evt.FinalScore}");
    }

    private void OnGameStateChanged(GameStateChangedEvent evt)
    {
        Debug.Log($"[GameLaunch] 状态变化: {evt.OldState} -> {evt.NewState}");
    }
}
