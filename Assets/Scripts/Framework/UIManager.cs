using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// UI管理器 - 管理所有UI面板
/// </summary>
public class UIManager : Singleton<UIManager>
{
    [Header("UI Panels")]
    public GameObject gamePanel;
    public GameObject winPanel;
    public GameObject losePanel;

    [Header("UI Texts")]
    public Text statusText;
    public Text levelText;
    public Text timerText;
    public Text moveCountText;

    [Header("UI Buttons")]
    public Button restartButton;
    public Button nextLevelButton;

    // 面板栈
    private Stack<UIPanel> panelStack = new Stack<UIPanel>();

    protected override void Awake()
    {
        base.Awake();
    }

    private void Start()
    {
        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestartClicked);

        if (nextLevelButton != null)
            nextLevelButton.onClick.AddListener(OnNextLevelClicked);

        // 订阅事件
        EventSystem.Instance.Subscribe<GameStartEvent>(OnGameStart);
        EventSystem.Instance.Subscribe<GameOverEvent>(OnGameOver);
    }

    protected override void OnDestroy()
    {
        EventSystem.Instance.Unsubscribe<GameStartEvent>(OnGameStart);
        EventSystem.Instance.Unsubscribe<GameOverEvent>(OnGameOver);
        base.OnDestroy();
    }

    // ========== 面板管理 ==========

    /// <summary>
    /// 显示面板
    /// </summary>
    public T ShowPanel<T>() where T : UIPanel
    {
        T panel = GetComponentInChildren<T>(true);
        if (panel != null)
        {
            panelStack.Push(panel);
            panel.OnShow();
        }
        return panel;
    }

    /// <summary>
    /// 隐藏当前面板
    /// </summary>
    public void HideCurrentPanel()
    {
        if (panelStack.Count > 0)
        {
            UIPanel panel = panelStack.Pop();
            panel.OnHide();
        }
    }

    /// <summary>
    /// 隐藏所有面板
    /// </summary>
    public void HideAllPanels()
    {
        while (panelStack.Count > 0)
        {
            UIPanel panel = panelStack.Pop();
            panel.OnHide();
        }
    }

    // ========== 文本更新 ==========

    public void UpdateStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }

    public void UpdateLevel(int level)
    {
        if (levelText != null)
            levelText.text = "Level " + level;
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
            moveCountText.text = "Moves: " + count;
    }

    // ========== 面板切换 ==========

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

    // ========== 按钮回调 ==========

    private void OnRestartClicked()
    {
        GameManager.Instance?.RestartGame();
    }

    private void OnNextLevelClicked()
    {
        GameManager.Instance?.NextLevel();
    }

    // ========== 事件处理 ==========

    private void OnGameStart(GameStartEvent evt)
    {
        ShowGamePanel();
        UpdateStatus("Game Started!");
    }

    private void OnGameOver(GameOverEvent evt)
    {
        if (evt.IsWin)
            ShowWinPanel();
        else
            ShowLosePanel();
    }
}
