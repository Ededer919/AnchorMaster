using UnityEngine;

/// <summary>
/// Manages level loading, progression, and restart. Singleton.
/// Loads LevelData assets from Resources/Levels/.
/// </summary>
public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("Level Assets")]
    public LevelData[] levels;

    public LevelData CurrentLevel => levels != null && currentIndex < levels.Length
        ? levels[currentIndex]
        : null;

    public int CurrentIndex => currentIndex;
    public int TotalLevels => levels != null ? levels.Length : 0;

    private int currentIndex = 0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // Auto-load from Resources if not assigned in Inspector
        if (levels == null || levels.Length == 0)
        {
            LoadFromResources();
        }

        if (levels != null && levels.Length > 0)
        {
            Debug.Log($"[LevelManager] {levels.Length} levels loaded. Current: {CurrentLevel?.levelName}");
        }
    }

    private void OnEnable()
    {
        EventSystem.Instance?.Subscribe<GameOverEvent>(OnGameOver);
    }

    private void OnDisable()
    {
        EventSystem.Instance?.Unsubscribe<GameOverEvent>(OnGameOver);
    }

    private void OnGameOver(GameOverEvent evt)
    {
        if (evt.IsWin)
        {
            if (IsLastLevel())
            {
                Debug.Log("[LevelManager] All levels complete!");
                UIManager.Instance?.UpdateStatus("All levels complete! You win!");
            }
            else
            {
                NextLevel();
            }
        }
    }

    private void LoadFromResources()
    {
        var assets = Resources.LoadAll<LevelData>("Levels");
        if (assets != null && assets.Length > 0)
        {
            levels = assets;
            Debug.Log($"[LevelManager] Loaded {assets.Length} levels from Resources/Levels/");
        }
    }

    public void LoadLevel(int index)
    {
        if (levels == null || index < 0 || index >= levels.Length) return;

        currentIndex = index;
        Debug.Log($"[LevelManager] Loaded level {index + 1}: {levels[index].levelName}");
    }

    public void NextLevel()
    {
        if (currentIndex < levels.Length - 1)
        {
            LoadLevel(currentIndex + 1);
        }
    }

    public void RestartLevel()
    {
        LoadLevel(currentIndex);
    }

    public bool IsLastLevel()
    {
        return currentIndex >= levels.Length - 1;
    }
}
