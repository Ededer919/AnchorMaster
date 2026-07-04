using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI Panels")]
    public GameObject gamePanel;
    public GameObject winPanel;
    public GameObject losePanel;

    [Header("UI Texts")]
    public Text statusText;
    public Text anchorCountText;
    public Text levelText;
    public Text timerText;
    public Text moveCountText;

    [Header("UI Buttons")]
    public Button confirmButton;
    public Button restartButton;
    public Button nextLevelButton;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnConfirmClicked);
        }

        if (restartButton != null)
        {
            restartButton.onClick.AddListener(OnRestartClicked);
        }

        if (nextLevelButton != null)
        {
            nextLevelButton.onClick.AddListener(OnNextLevelClicked);
        }

        EnableConfirmButton(false);
    }

    public void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    public void UpdateAnchorCount(int current, int max)
    {
        if (anchorCountText != null)
        {
            anchorCountText.text = "Anchors: " + current + " / " + max;
        }
    }

    public void UpdateLevel(int level)
    {
        if (levelText != null)
        {
            levelText.text = "Level " + level;
        }
    }

    public void UpdateTimer(float time)
    {
        if (timerText != null)
        {
            int minutes = (int)(time / 60);
            int seconds = (int)(time % 60);
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }

    public void UpdateMoveCount(int count)
    {
        if (moveCountText != null)
        {
            moveCountText.text = "Moves: " + count;
        }
    }

    public void EnableConfirmButton(bool enable)
    {
        if (confirmButton != null)
        {
            confirmButton.interactable = enable;
        }
    }

    public void ShowGamePanel()
    {
        if (gamePanel != null) gamePanel.SetActive(true);
        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
    }

    public void ShowWinPanel()
    {
        if (gamePanel != null) gamePanel.SetActive(false);
        if (winPanel != null) winPanel.SetActive(true);
    }

    public void ShowLosePanel()
    {
        if (gamePanel != null) gamePanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(true);
    }

    private void OnConfirmClicked()
    {
        GameManager.Instance?.ConfirmAnchorPlacement();
    }

    private void OnRestartClicked()
    {
        GameManager.Instance?.RestartGame();
    }

    private void OnNextLevelClicked()
    {
        GameManager.Instance?.NextLevel();
    }
}
