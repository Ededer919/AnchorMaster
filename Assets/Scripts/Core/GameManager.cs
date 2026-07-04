using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Settings")]
    public int gridSize = 4;

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
        Debug.Log("Game Initialized - Grid Size: " + gridSize);
        UIManager.Instance?.UpdateStatus("Game Started!");
    }

    public void OnPuzzleSolved()
    {
        Debug.Log("Puzzle Solved!");
        UIManager.Instance?.ShowWinPanel();
    }

    public void RestartGame()
    {
        InitializeGame();
    }
}
