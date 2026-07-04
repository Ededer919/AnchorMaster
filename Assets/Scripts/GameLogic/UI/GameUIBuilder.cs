using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Loads game UI from prefab, caches references, wires button events.
/// Cards trigger toolbar drag via GridInputHandler.StartToolbarDrag.
/// </summary>
public class GameUIBuilder : MonoBehaviour
{
    private const string PREFAB_PATH = "UI/Panels/Game";

    public Text StatusText { get; private set; }
    public Text LevelText { get; private set; }
    public Text TimerText { get; private set; }
    public Button WindButton { get; private set; }
    public Button BuildModeButton { get; private set; }
    public Button RetryButton { get; private set; }
    public Button NextButton { get; private set; }
    public GameObject ResultPanel { get; private set; }
    public Text ResultTitle { get; private set; }
    public Text ResultSubtitle { get; private set; }

    private readonly List<ObjectCard> cards = new List<ObjectCard>();
    private BuildSystem buildSystem;
    private GridInputHandler inputHandler;

    private struct ObjectCard
    {
        public ObjectType type;
        public Button button;
        public Image iconImage;
        public Text nameText;
        public Text countText;
        public Text descText;
    }

    public void CreateUI(BuildSystem buildSys, DisasterWindController windController)
    {
        buildSystem = buildSys;
        _ = windController; // wind is now auto-triggered by GameLauncher, not UI

        // Ensure EventSystem
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGo.AddComponent<StandaloneInputModule>();
        }

        // Find existing GamePanel (loaded by GameFlowManager) or load prefab
        GameObject canvasGo = GameObject.Find("GamePanel");
        if (canvasGo == null) canvasGo = GameObject.Find("Game");

        if (canvasGo == null)
        {
            // Load prefab ourselves
            var prefab = Resources.Load<GameObject>(PREFAB_PATH);
            if (prefab == null)
            {
                Debug.LogError("[GameUIBuilder] Prefab not found: " + PREFAB_PATH);
                return;
            }
            canvasGo = Instantiate(prefab);
            canvasGo.name = "GamePanel";
        }

        // Cache references
        LevelText = Find<Text>(canvasGo, "TopBar/LevelText");
        TimerText = Find<Text>(canvasGo, "TopBar/TimerText");
        StatusText = Find<Text>(canvasGo, "TopBar/StatusText");

        // Disable decorative full-screen background and sun — they block game world rendering
        var bgGo = FindGo(canvasGo, "bg");
        if (bgGo != null) bgGo.SetActive(false);
        var sunGo = FindGo(canvasGo, "sun");
        if (sunGo != null) sunGo.SetActive(false);

        WindButton = Find<Button>(canvasGo, "WindButton");
        BuildModeButton = Find<Button>(canvasGo, "BuildModeButton");
        ResultPanel = FindGo(canvasGo, "ResultScreen");
        ResultTitle = Find<Text>(canvasGo, "ResultScreen/ResultCard/ResultTitle");
        ResultSubtitle = Find<Text>(canvasGo, "ResultScreen/ResultCard/ResultSubtitle");
        RetryButton = Find<Button>(canvasGo, "ResultScreen/ResultCard/ButtonRow/RetryButton");
        NextButton = Find<Button>(canvasGo, "ResultScreen/ResultCard/ButtonRow/NextButton");

        // Debug missing refs
        if (LevelText == null) Debug.LogError("[GameUIBuilder] LevelText not found at TopBar/LevelText");
        if (TimerText == null) Debug.LogError("[GameUIBuilder] TimerText not found at TopBar/TimerText");

        // Link to UIManager
        if (UIManager.Instance != null && StatusText != null)
            UIManager.Instance.statusText = StatusText;

        // Wire buttons
        if (BuildModeButton != null)
        {
            BuildModeButton.onClick.AddListener(() =>
            {
                buildSystem.ToggleMode();
                SetText(BuildModeButton, buildSystem.IsBuildMode ? "自由模式" : "网格模式");
            });
        }

        if (RetryButton != null)
        {
            RetryButton.onClick.AddListener(() =>
            {
                HideResultScreen();
                GameFlowManager.Instance?.ReturnToLevelChoose();
            });
        }

        if (NextButton != null)
        {
            NextButton.onClick.AddListener(() =>
            {
                HideResultScreen();
                if (LevelManager.Instance != null && !LevelManager.Instance.IsLastLevel())
                {
                    LevelManager.Instance.NextLevel();
                    GameFlowManager.Instance?.StartNextLevel();
                }
                else
                {
                    GameFlowManager.Instance?.ReturnToLevelChoose();
                }
            });
        }

        // Cache input handler for toolbar drag
        inputHandler = FindFirstObjectByType<GridInputHandler>();

        // Cache object cards from LeftSidebar
        var toolList = FindGo(canvasGo, "LeftSidebar/ToolList");
        if (toolList == null) toolList = FindGo(canvasGo, "BottomBar/ObjectToolbar");

        if (toolList != null)
        {
            for (int i = 0; i < System.Enum.GetValues(typeof(ObjectType)).Length; i++)
            {
                var type = (ObjectType)i;

                // Skip Bedrock — auto-placed, not in toolbar
                if (type == ObjectType.Bedrock) continue;

                var cardGo = FindGo(toolList.transform, "Card_" + i);
                if (cardGo == null) continue;

                var btn = cardGo.GetComponent<Button>();
                var icon = Find<Image>(cardGo.transform, "Icon");
                var nameT = Find<Text>(cardGo.transform, "TopRow/Name");
                if (nameT == null) nameT = Find<Text>(cardGo.transform, "Name");
                var countT = Find<Text>(cardGo.transform, "TopRow/Count");
                if (countT == null) countT = Find<Text>(cardGo.transform, "Count");
                var descT = Find<Text>(cardGo.transform, "Desc");

                var config = ObjectConfig.Get(type);
                if (nameT != null) nameT.text = config.name;
                if (descT != null) descT.text = config.description;
                if (icon != null)
                {
                    var prefab = Resources.Load<GameObject>("UI/Prefabs/Objects/" + type.ToString());
                    if (prefab != null)
                    {
                        var sr = prefab.GetComponent<SpriteRenderer>();
                        if (sr != null && sr.sprite != null) icon.sprite = sr.sprite;
                    }
                }

                cards.Add(new ObjectCard { type = type, button = btn, iconImage = icon, nameText = nameT, countText = countT, descText = descT });

                // Click card → auto-spawn near balloon
                if (btn != null)
                {
                    var capturedType = type;
                    btn.onClick.AddListener(() =>
                    {
                        buildSystem.SpawnNearBalloon(capturedType);
                    });
                }
            }
        }

        // Hide result screen
        if (ResultPanel != null)
            ResultPanel.SetActive(false);

        Debug.Log("[GameUIBuilder] UI loaded from prefab.");
    }

    public void UpdateObjectCounts(BuildSystem buildSys)
    {
        foreach (var card in cards)
        {
            int count = buildSys.GetCount(card.type);
            int quota = buildSys.GetQuota(card.type);
            int available = quota - count;

            if (card.countText != null)
                card.countText.text = available + "/" + quota;

            if (card.button != null)
                card.button.interactable = available > 0;

            if (card.nameText != null)
                card.nameText.color = available <= 0 ? new Color(0.4f, 0.4f, 0.4f) : Color.white;
        }
    }

    public void SetLevelText(string text)
    {
        if (LevelText != null)
            LevelText.text = text;
    }

    public void UpdateTimer(float remaining)
    {
        if (TimerText != null)
        {
            int seconds = Mathf.CeilToInt(remaining);
            TimerText.text = "建造时间: " + seconds + "s";
            TimerText.color = seconds <= 5 ? new Color(1f, 0.3f, 0.2f) : new Color(1f, 0.85f, 0.2f);
        }
    }

    public void SetWindPhase()
    {
        if (TimerText != null)
        {
            TimerText.text = "暴风中!";
            TimerText.color = new Color(1f, 0.3f, 0.2f);
        }
        // Disable all card buttons
        foreach (var card in cards)
        {
            if (card.button != null)
                card.button.interactable = false;
        }
    }

    public void SetBuildPhaseActive(bool active)
    {
        if (BuildModeButton != null)
            BuildModeButton.interactable = active;
    }

    public void UpdateWindButtonLabel(bool windActive)
    {
        // Wind is now auto-triggered; kept for compatibility
    }

    public void ShowResultScreen(bool isWin, string message)
    {
        if (ResultPanel == null) return;

        if (ResultTitle != null)
        {
            ResultTitle.text = isWin ? "胜利!" : "失败!";
            ResultTitle.color = isWin ? new Color(0.3f, 1f, 0.4f) : new Color(1f, 0.3f, 0.3f);
        }
        if (ResultSubtitle != null)
            ResultSubtitle.text = message;

        if (RetryButton != null)
        {
            SetText(RetryButton, "返回选关");
            RetryButton.gameObject.SetActive(true);
        }

        if (NextButton != null)
        {
            bool showNext = isWin && LevelManager.Instance != null && !LevelManager.Instance.IsLastLevel();
            NextButton.gameObject.SetActive(showNext);
            if (showNext) SetText(NextButton, "下一关");
        }

        ResultPanel.SetActive(true);
    }

    public void HideResultScreen()
    {
        if (ResultPanel != null) ResultPanel.SetActive(false);
    }

    // === Helpers ===
    private T Find<T>(GameObject root, string path) where T : Component
    {
        var t = root.transform.Find(path);
        return t != null ? t.GetComponent<T>() : null;
    }

    private T Find<T>(Transform root, string name) where T : Component
    {
        var t = root.Find(name);
        return t != null ? t.GetComponent<T>() : null;
    }

    private GameObject FindGo(GameObject root, string path)
    {
        var t = root.transform.Find(path);
        return t != null ? t.gameObject : null;
    }

    private GameObject FindGo(Transform root, string name)
    {
        var t = root.Find(name);
        return t != null ? t.gameObject : null;
    }

    private void SetText(Button button, string text)
    {
        if (button == null) return;
        var label = button.GetComponentInChildren<Text>();
        if (label != null) label.text = text;
    }
}
