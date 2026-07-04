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
    private const string PREFAB_PATH = "UI/Prefabs/GameUICanvas";

    public Text StatusText { get; private set; }
    public Text LevelText { get; private set; }
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
        public Text nameText;
        public Text countText;
    }

    public void CreateUI(BuildSystem buildSys, DisasterWindController windController)
    {
        buildSystem = buildSys;

        // Ensure EventSystem
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGo.AddComponent<StandaloneInputModule>();
        }

        // Remove old canvas
        var oldCanvas = GameObject.Find("GameUICanvas");
        if (oldCanvas != null) DestroyImmediate(oldCanvas);
        var oldSceneCanvas = GameObject.Find("Canvas");
        if (oldSceneCanvas != null) DestroyImmediate(oldSceneCanvas);

        // Load prefab
        var prefab = Resources.Load<GameObject>(PREFAB_PATH);
        if (prefab == null)
        {
            Debug.LogError("[GameUIBuilder] Prefab not found: " + PREFAB_PATH);
            return;
        }

        var canvasGo = Instantiate(prefab);
        canvasGo.name = "GameUICanvas";

        // Cache references
        LevelText = Find<Text>(canvasGo, "TopBar/LevelText");
        StatusText = Find<Text>(canvasGo, "TopBar/StatusText");
        WindButton = Find<Button>(canvasGo, "WindButton");
        BuildModeButton = Find<Button>(canvasGo, "BuildModeButton");
        ResultPanel = FindGo(canvasGo, "ResultScreen");
        ResultTitle = Find<Text>(canvasGo, "ResultScreen/ResultCard/ResultTitle");
        ResultSubtitle = Find<Text>(canvasGo, "ResultScreen/ResultCard/ResultSubtitle");
        RetryButton = Find<Button>(canvasGo, "ResultScreen/ResultCard/ButtonRow/RetryButton");
        NextButton = Find<Button>(canvasGo, "ResultScreen/ResultCard/ButtonRow/NextButton");

        // Debug missing refs
        if (LevelText == null) Debug.LogError("[GameUIBuilder] LevelText not found at TopBar/LevelText");
        if (StatusText == null) Debug.LogError("[GameUIBuilder] StatusText not found at TopBar/StatusText");
        if (WindButton == null) Debug.LogError("[GameUIBuilder] WindButton not found");

        // Link to UIManager
        if (UIManager.Instance != null && StatusText != null)
            UIManager.Instance.statusText = StatusText;

        // Wire buttons
        if (WindButton != null)
            WindButton.onClick.AddListener(windController.ToggleWind);

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
                FindFirstObjectByType<GameLauncher>()?.ReloadScene();
            });
        }

        if (NextButton != null)
        {
            NextButton.onClick.AddListener(() =>
            {
                HideResultScreen();
                LevelManager.Instance?.NextLevel();
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
                var nameT = Find<Text>(cardGo.transform, "TopRow/Name");
                if (nameT == null) nameT = Find<Text>(cardGo.transform, "Name");
                var countT = Find<Text>(cardGo.transform, "Count");

                cards.Add(new ObjectCard { type = type, button = btn, nameText = nameT, countText = countT });

                // Click card → start toolbar drag
                if (btn != null)
                {
                    var capturedType = type;
                    btn.onClick.AddListener(() =>
                    {
                        if (inputHandler != null)
                        {
                            // Use current mouse position as screen pos
                            Vector2 mousePos = Input.mousePosition;
                            inputHandler.StartToolbarDrag(capturedType, mousePos);
                        }
                        else
                        {
                            buildSystem.SpawnSpecificType(capturedType);
                        }
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
        else
            Debug.LogError("[GameUIBuilder] SetLevelText called but LevelText is null");
    }

    public void UpdateWindButtonLabel(bool windActive)
    {
        if (WindButton != null)
        {
            SetText(WindButton, windActive ? "停止暴风" : "开始暴风");
            var img = WindButton.GetComponent<Image>();
            if (img != null)
                img.color = windActive ? new Color(0.65f, 0.15f, 0.1f, 0.95f) : new Color(0.15f, 0.45f, 0.15f, 0.95f);
        }
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

        if (NextButton != null)
        {
            bool showNext = isWin && LevelManager.Instance != null && !LevelManager.Instance.IsLastLevel();
            NextButton.gameObject.SetActive(showNext);
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
