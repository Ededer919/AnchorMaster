using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Settings")]
    public int gridSize = 4;

    [Header("Level Settings")]
    public int currentLevel = 1;
    public bool isGameActive = false;

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
        }
    }

    private void Start()
    {
        InitializeGame();
    }

    public void InitializeGame()
    {
        isGameActive = true;
        Debug.Log("Game Initialized - Level: " + currentLevel);

        UIManager.Instance?.UpdateStatus("Place your anchors!");
        UIManager.Instance?.ShowGamePanel();
    }

    public void ConfirmAnchorPlacement()
    {
        if (!isGameActive) return;

        isGameActive = false;
        UIManager.Instance?.UpdateStatus("Disaster incoming!");

        StartDisaster();
    }

    private void StartDisaster()
    {
        Debug.Log("Disaster started!");
        StartCoroutine(DisasterCoroutine());
    }

    private System.Collections.IEnumerator DisasterCoroutine()
    {
        yield return new WaitForSeconds(10f);

        if (isGameActive)
        {
            LevelComplete();
        }
    }

    public void LevelComplete()
    {
        Debug.Log("Level " + currentLevel + " completed!");
        isGameActive = false;

        UIManager.Instance?.ShowWinPanel();
        UIManager.Instance?.UpdateStatus("Level Complete!");
    }

    public void LevelFail()
    {
        Debug.Log("Level " + currentLevel + " failed!");
        isGameActive = false;

        UIManager.Instance?.ShowLosePanel();
        UIManager.Instance?.UpdateStatus("Level Failed!");
    }

    public void RestartGame()
    {
        currentLevel = 1;
        InitializeGame();

        AnchorManager.Instance?.RemoveAllAnchors();
    }

    public void NextLevel()
    {
        currentLevel++;
        InitializeGame();

        AnchorManager.Instance?.InitializeAnchors();
    }
}
