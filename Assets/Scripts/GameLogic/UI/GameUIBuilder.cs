using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Builds game UI: status bar, object toolbar (with type/description), control buttons.
/// Single canvas, all UI elements properly laid out.
/// </summary>
public class GameUIBuilder : MonoBehaviour
{
    public Text StatusText { get; private set; }
    public Text LevelText { get; private set; }
    public Button WindButton { get; private set; }
    public Button BuildModeButton { get; private set; }

    private readonly List<ObjectCard> cards = new List<ObjectCard>();
    private BuildSystem buildSystem;

    private struct ObjectCard
    {
        public ObjectType type;
        public Button button;
        public Text nameText;
        public Text countText;
        public Text descText;
        public Image icon;
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

        // Create single Canvas
        var canvasGo = new GameObject("GameUICanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGo.AddComponent<GraphicRaycaster>();

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // === Top Bar: Level name + Status ===
        var topBar = CreatePanel(canvasGo.transform, "TopBar",
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1f), new Vector2(0, -60), new Vector2(0, 50));
        var topBarImg = topBar.AddComponent<Image>();
        topBarImg.color = new Color(0.08f, 0.08f, 0.12f, 0.9f);

        LevelText = CreateText(topBar.transform, "LevelText", "", font, 16, TextAnchor.MiddleLeft,
            new Vector2(0, 0), new Vector2(0.3f, 1), new Vector2(20, 0), Vector2.zero);
        LevelText.color = new Color(0.8f, 0.8f, 1f);

        StatusText = CreateText(topBar.transform, "StatusText", "Loading...", font, 16, TextAnchor.MiddleCenter,
            new Vector2(0.3f, 0), new Vector2(1, 1), new Vector2(20, 0), new Vector2(-20, 0));
        StatusText.color = Color.white;

        // Link to UIManager
        if (UIManager.Instance != null)
            UIManager.Instance.statusText = StatusText;

        // === Bottom: Object Toolbar + Buttons ===
        var bottomBar = CreatePanel(canvasGo.transform, "BottomBar",
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0f), new Vector2(0, 10), new Vector2(0, 140));
        var bottomImg = bottomBar.AddComponent<Image>();
        bottomImg.color = new Color(0.06f, 0.06f, 0.1f, 0.92f);

        // Object toolbar label
        var toolbarLabel = CreateText(bottomBar.transform, "ToolbarLabel", "建材栏", font, 13, TextAnchor.UpperLeft,
            new Vector2(0, 1), new Vector2(0.15f, 1), new Vector2(10, -5), Vector2.zero);
        toolbarLabel.color = new Color(0.6f, 0.6f, 0.7f);

        // Create object cards
        CreateObjectToolbar(bottomBar.transform, font);

        // === Control Buttons (right side of bottom bar) ===
        // Start Wind button
        WindButton = CreateButton(bottomBar.transform, "WindButton", "开始暴风", font, 15,
            new Vector2(1f, 0.5f), new Vector2(1f, 1f), new Vector2(-10, -5), new Vector2(120, 60),
            new Color(0.15f, 0.45f, 0.15f, 0.95f));
        WindButton.onClick.AddListener(windController.ToggleWind);

        // Build Mode button
        BuildModeButton = CreateButton(bottomBar.transform, "BuildModeButton", "网格模式", font, 14,
            new Vector2(1f, 0f), new Vector2(1f, 0.5f), new Vector2(-10, 5), new Vector2(120, 55),
            new Color(0.2f, 0.2f, 0.4f, 0.95f));
        BuildModeButton.onClick.AddListener(() =>
        {
            buildSystem.ToggleMode();
            SetButtonText(BuildModeButton, buildSystem.IsBuildMode ? "自由模式" : "网格模式");
        });

        // Create result screen (hidden by default)
        CreateResultScreen(canvasGo.transform);

        Debug.Log("[GameUIBuilder] UI created.");
    }

    private GameObject resultPanel;

    private void CreateResultScreen(Transform canvasParent)
    {
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Full-screen dim background
        resultPanel = new GameObject("ResultScreen");
        resultPanel.transform.SetParent(canvasParent, false);
        var rect = resultPanel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        var bg = resultPanel.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.8f);

        // Result card (center)
        var cardGo = new GameObject("ResultCard");
        cardGo.transform.SetParent(resultPanel.transform, false);
        var cardRect = cardGo.AddComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(500, 250);
        var cardImg = cardGo.AddComponent<Image>();
        cardImg.color = new Color(0.1f, 0.1f, 0.16f, 0.95f);

        var cardLayout = cardGo.AddComponent<VerticalLayoutGroup>();
        cardLayout.spacing = 15;
        cardLayout.padding = new RectOffset(20, 20, 25, 20);
        cardLayout.childAlignment = TextAnchor.MiddleCenter;

        // Title text
        var titleGo = new GameObject("ResultTitle");
        titleGo.transform.SetParent(cardGo.transform, false);
        var titleLayout = titleGo.AddComponent<LayoutElement>();
        titleLayout.preferredHeight = 50;
        resultTitleText = titleGo.AddComponent<Text>();
        resultTitleText.font = font;
        resultTitleText.fontSize = 36;
        resultTitleText.alignment = TextAnchor.MiddleCenter;
        resultTitleText.color = Color.white;

        // Subtitle text
        var subGo = new GameObject("ResultSubtitle");
        subGo.transform.SetParent(cardGo.transform, false);
        var subLayout = subGo.AddComponent<LayoutElement>();
        subLayout.preferredHeight = 40;
        resultSubText = subGo.AddComponent<Text>();
        resultSubText.font = font;
        resultSubText.fontSize = 20;
        resultSubText.alignment = TextAnchor.MiddleCenter;
        resultSubText.color = new Color(0.7f, 0.7f, 0.75f);

        // Button row
        var btnRowGo = new GameObject("ButtonRow");
        btnRowGo.transform.SetParent(cardGo.transform, false);
        var btnRowRect = btnRowGo.AddComponent<RectTransform>();
        var btnRowLayout = btnRowGo.AddComponent<HorizontalLayoutGroup>();
        btnRowLayout.spacing = 20;
        btnRowLayout.childAlignment = TextAnchor.MiddleCenter;
        btnRowLayout.childForceExpandWidth = false;

        // Retry button
        var retryBtn = new GameObject("RetryButton");
        retryBtn.transform.SetParent(btnRowGo.transform, false);
        var retryLayout = retryBtn.AddComponent<LayoutElement>();
        retryLayout.preferredWidth = 140;
        retryLayout.preferredHeight = 45;
        var retryImg = retryBtn.AddComponent<Image>();
        retryImg.color = new Color(0.3f, 0.4f, 0.6f, 1f);
        var retryButton = retryBtn.AddComponent<Button>();
        var retryLabel = new GameObject("Label");
        retryLabel.transform.SetParent(retryBtn.transform, false);
        var retryLabelRect = retryLabel.AddComponent<RectTransform>();
        retryLabelRect.anchorMin = Vector2.zero;
        retryLabelRect.anchorMax = Vector2.one;
        retryLabelRect.offsetMin = Vector2.zero;
        retryLabelRect.offsetMax = Vector2.zero;
        var retryText = retryLabel.AddComponent<Text>();
        retryText.text = "重试";
        retryText.font = font;
        retryText.fontSize = 18;
        retryText.alignment = TextAnchor.MiddleCenter;
        retryText.color = Color.white;
        retryButton.onClick.AddListener(() =>
        {
            HideResultScreen();
            FindFirstObjectByType<GameLauncher>()?.ReloadScene();
        });

        // Next level button (only shown on win)
        var nextGo = new GameObject("NextButton");
        nextGo.transform.SetParent(btnRowGo.transform, false);
        var nextLayout = nextGo.AddComponent<LayoutElement>();
        nextLayout.preferredWidth = 140;
        nextLayout.preferredHeight = 45;
        var nextImg = nextGo.AddComponent<Image>();
        nextImg.color = new Color(0.15f, 0.5f, 0.15f, 1f);
        nextBtn = nextGo.AddComponent<Button>();
        var nextLabel = new GameObject("Label");
        nextLabel.transform.SetParent(nextGo.transform, false);
        var nextLabelRect = nextLabel.AddComponent<RectTransform>();
        nextLabelRect.anchorMin = Vector2.zero;
        nextLabelRect.anchorMax = Vector2.one;
        nextLabelRect.offsetMin = Vector2.zero;
        nextLabelRect.offsetMax = Vector2.zero;
        var nextText = nextLabel.AddComponent<Text>();
        nextText.text = "下一关";
        nextText.font = font;
        nextText.fontSize = 18;
        nextText.alignment = TextAnchor.MiddleCenter;
        nextText.color = Color.white;
        nextBtn.onClick.AddListener(() =>
        {
            HideResultScreen();
            LevelManager.Instance?.NextLevel();
        });

        resultPanel.SetActive(false);
    }

    private Text resultTitleText;
    private Text resultSubText;
    private Button nextBtn;

    public void ShowResultScreen(bool isWin, string message)
    {
        if (resultPanel == null) return;

        resultTitleText.text = isWin ? "胜利!" : "失败!";
        resultTitleText.color = isWin ? new Color(0.3f, 1f, 0.4f) : new Color(1f, 0.3f, 0.3f);
        resultSubText.text = message;

        // Show/hide next button
        nextBtn.gameObject.SetActive(isWin && !LevelManager.Instance?.IsLastLevel() != false);

        resultPanel.SetActive(true);
    }

    public void HideResultScreen()
    {
        if (resultPanel != null)
            resultPanel.SetActive(false);
    }

    private void CreateObjectToolbar(Transform parent, Font font)
    {
        // Horizontal scroll area for object cards
        var scrollGo = new GameObject("ObjectToolbar");
        scrollGo.transform.SetParent(parent, false);
        var scrollRect = scrollGo.AddComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0, 0);
        scrollRect.anchorMax = new Vector2(0.78f, 1);
        scrollRect.offsetMin = new Vector2(10, 5);
        scrollRect.offsetMax = new Vector2(-5, -20);

        // Use HorizontalLayoutGroup for auto-arrangement
        var hlg = scrollGo.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        var fitter = scrollGo.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Create a card for each object type
        for (int i = 0; i < System.Enum.GetValues(typeof(ObjectType)).Length; i++)
        {
            var type = (ObjectType)i;
            var config = ObjectConfig.Get(type);

            var cardGo = new GameObject($"Card_{config.name}");
            cardGo.transform.SetParent(scrollGo.transform, false);

            var cardRect = cardGo.AddComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(95, 110);

            var cardLayout = cardGo.AddComponent<VerticalLayoutGroup>();
            cardLayout.spacing = 2;
            cardLayout.padding = new RectOffset(4, 4, 4, 4);
            cardLayout.childAlignment = TextAnchor.UpperCenter;
            cardLayout.childForceExpandWidth = true;
            cardLayout.childForceExpandHeight = false;

            var cardImg = cardGo.AddComponent<Image>();
            cardImg.color = new Color(0.12f, 0.12f, 0.18f, 0.95f);

            var cardButton = cardGo.AddComponent<Button>();
            var colors = cardButton.colors;
            colors.normalColor = new Color(0.15f, 0.15f, 0.22f, 1f);
            colors.highlightedColor = new Color(0.25f, 0.25f, 0.35f, 1f);
            colors.pressedColor = new Color(0.1f, 0.1f, 0.15f, 1f);
            cardButton.colors = colors;

            // Icon (color square)
            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(cardGo.transform, false);
            var iconRect = iconGo.AddComponent<RectTransform>();
            iconRect.sizeDelta = new Vector2(30, 30);
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.color = config.color;
            var iconLayout = iconGo.AddComponent<LayoutElement>();
            iconLayout.preferredWidth = 30;
            iconLayout.preferredHeight = 30;

            // Name
            var nameText = CreateText(cardGo.transform, "Name", config.name, font, 12, TextAnchor.UpperCenter,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            nameText.color = Color.white;

            // Description
            var descText = CreateText(cardGo.transform, "Desc", config.description, font, 9, TextAnchor.UpperCenter,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            descText.color = new Color(0.6f, 0.6f, 0.65f);
            descText.lineSpacing = 0.8f;

            // Count
            var countText = CreateText(cardGo.transform, "Count", "0/0", font, 10, TextAnchor.UpperCenter,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            countText.color = new Color(0.4f, 0.8f, 0.4f);

            var card = new ObjectCard
            {
                type = type,
                button = cardButton,
                nameText = nameText,
                countText = countText,
                descText = descText,
                icon = iconImg
            };
            cards.Add(card);

            // Click → spawn this specific type
            cardButton.onClick.AddListener(() =>
            {
                buildSystem.SpawnSpecificType(card.type);
            });
        }
    }

    /// <summary>
    /// Updates all object cards' count display.
    /// </summary>
    public void UpdateObjectCounts(BuildSystem buildSys)
    {
        foreach (var card in cards)
        {
            int count = buildSys.GetCount(card.type);
            int quota = buildSys.GetQuota(card.type);
            card.countText.text = $"{count}/{quota}";

            // Grey out if maxed
            if (count >= quota)
            {
                card.button.interactable = false;
                card.nameText.color = new Color(0.4f, 0.4f, 0.4f);
            }
            else
            {
                card.button.interactable = true;
                card.nameText.color = Color.white;
            }
        }
    }

    public void SetLevelText(string text)
    {
        if (LevelText != null)
            LevelText.text = text;
    }

    public void UpdateWindButtonLabel(bool windActive)
    {
        if (WindButton != null)
        {
            SetButtonText(WindButton, windActive ? "停止暴风" : "开始暴风");
            var img = WindButton.GetComponent<Image>();
            if (img != null)
                img.color = windActive
                    ? new Color(0.65f, 0.15f, 0.1f, 0.95f)
                    : new Color(0.15f, 0.45f, 0.15f, 0.95f);
        }
    }

    private GameObject CreatePanel(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 position, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return go;
    }

    private Text CreateText(Transform parent, string name, string text, Font font, int fontSize,
        TextAnchor alignment, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        var t = go.AddComponent<Text>();
        t.text = text;
        t.font = font;
        t.fontSize = fontSize;
        t.alignment = alignment;
        t.raycastTarget = false;
        return t;
    }

    private Button CreateButton(Transform parent, string name, string text, Font font, int fontSize,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(1f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        var img = go.AddComponent<Image>();
        img.color = color;

        var btn = go.AddComponent<Button>();

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(go.transform, false);
        var labelRect = labelGo.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        var label = labelGo.AddComponent<Text>();
        label.text = text;
        label.font = font;
        label.fontSize = fontSize;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;

        return btn;
    }

    private void SetButtonText(Button button, string text)
    {
        if (button == null) return;
        var label = button.GetComponentInChildren<Text>();
        if (label != null)
            label.text = text;
    }
}
