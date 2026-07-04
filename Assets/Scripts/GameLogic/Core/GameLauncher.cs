using UnityEngine;

/// <summary>
/// Game entry point: initializes all subsystems in order, forwards framework events.
/// Reads level config from LevelManager. Attach to an empty GameObject in the scene.
/// </summary>
public class GameLauncher : MonoBehaviour
{
    private SceneBuilder sceneBuilder;
    private GridInputHandler inputHandler;
    private BuildSystem buildSystem;
    private GameUIBuilder uiBuilder;
    private PhysicsMergeSystem physicsMerge;
    private DisasterWindController windController;

    private void OnEnable()
    {
        EventSystem.Instance.Subscribe<GameStartEvent>(OnGameStart);
        EventSystem.Instance.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
    }

    private void OnDisable()
    {
        EventSystem.Instance.Unsubscribe<GameStartEvent>(OnGameStart);
        EventSystem.Instance.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
    }

    private void Start()
    {
        InitScene();
    }

    private void InitScene()
    {
        // Get level config
        LevelData level = LevelManager.Instance?.CurrentLevel;

        int rows = level != null ? level.rows : 5;
        int cols = level != null ? level.cols : 5;
        float cellSize = level != null ? level.cellSize : 1f;
        float groundWidth = level != null ? level.groundWidth : 30f;
        float groundHeight = level != null ? level.groundHeight : 4f;
        float windStrength = level != null ? level.windStrength : 5f;
        float naturalStrength = level != null ? level.naturalStrength : 2f;
        float windDuration = level != null ? level.windDuration : 10f;
        Vector2 windDir = level != null ? level.windDirection : Vector2.right;

        // 1. Build scene
        sceneBuilder = gameObject.AddComponent<SceneBuilder>();
        sceneBuilder.rows = rows;
        sceneBuilder.cols = cols;
        sceneBuilder.cellSize = cellSize;
        sceneBuilder.groundWidth = groundWidth;
        sceneBuilder.groundHeight = groundHeight;
        sceneBuilder.balloonCount = level != null ? level.humanCount : 1;
        sceneBuilder.Build();

        // 2. Physics merge system
        physicsMerge = gameObject.AddComponent<PhysicsMergeSystem>();
        physicsMerge.Initialize(sceneBuilder.ObjectManager, sceneBuilder.GridSystem);
        physicsMerge.SetWindSystem(sceneBuilder.WindSystem);

        // 3. Build system
        buildSystem = gameObject.AddComponent<BuildSystem>();
        buildSystem.Initialize(sceneBuilder, sceneBuilder.ObjectManager, level);
        physicsMerge.SetBuildSystem(buildSystem);

        // 4. Input handler
        inputHandler = gameObject.AddComponent<GridInputHandler>();
        inputHandler.Initialize(sceneBuilder.GridSystem, sceneBuilder.ObjectManager,
            sceneBuilder.GroundTopY, sceneBuilder.cellSize);
        inputHandler.SetBuildSystem(buildSystem);
        inputHandler.EnableInput();

        // 5. UI
        uiBuilder = gameObject.AddComponent<GameUIBuilder>();

        // 6. Wind controller
        windController = gameObject.AddComponent<DisasterWindController>();
        windController.disasterWindStrength = windStrength;
        windController.disasterNaturalStrength = naturalStrength;
        windController.disasterDuration = windDuration;
        sceneBuilder.WindSystem.globalWindDirection = windDir;
        sceneBuilder.WindSystem.windBounds = sceneBuilder.GridBounds;
        windController.Initialize(sceneBuilder.WindSystem, physicsMerge, uiBuilder, sceneBuilder.GroundTopY, sceneBuilder.GridBounds);
        uiBuilder.CreateUI(buildSystem, windController);
        uiBuilder.SetLevelText($"关卡 {level?.level ?? 1}: {level?.levelName ?? "默认"}");
        uiBuilder.UpdateObjectCounts(buildSystem);
        inputHandler.SetUIBuilder(uiBuilder);

        string levelName = level != null ? level.levelName : "Default";
        Debug.Log($"[GameLauncher] Level: {levelName}, Grid {rows}x{cols}, wind={windStrength}/{windDuration}s");
    }

    /// <summary>
    /// Called by LevelManager to rebuild scene for a new level.
    /// </summary>
    public void ReloadScene()
    {
        // Cleanup old subsystems
        inputHandler?.DisableInput();
        DestroyImmediate(sceneBuilder);
        DestroyImmediate(physicsMerge);
        DestroyImmediate(buildSystem);
        DestroyImmediate(inputHandler);
        DestroyImmediate(uiBuilder);
        DestroyImmediate(windController);

        // Destroy old GameWorld
        var oldWorld = GameObject.Find("GameWorld");
        if (oldWorld != null) DestroyImmediate(oldWorld);

        var oldCanvas = GameObject.Find("GameUICanvas");
        if (oldCanvas != null) DestroyImmediate(oldCanvas);

        // Rebuild
        InitScene();
        GameManager.Instance?.RestartGame();
    }

    private void OnGameStart(GameStartEvent evt)
    {
        LevelData level = LevelManager.Instance?.CurrentLevel;
        string name = level != null ? level.levelName : "Default";
        UIManager.Instance?.UpdateStatus($"{name} — Place objects to protect humans!");
        Debug.Log("[GameLauncher] Game started.");
    }

    private void OnGameStateChanged(GameStateChangedEvent evt)
    {
        if (evt.NewState == GameState.GameOver)
            windController.StopWind();
    }

    private void OnDestroy()
    {
        inputHandler?.DisableInput();
    }
}
