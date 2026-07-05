using UnityEngine;

/// <summary>
/// Game entry point: initializes all subsystems in order, forwards framework events.
/// Reads level config from LevelManager. Attach to an empty GameObject in the scene.
/// </summary>
public class GameLauncher : MonoBehaviour
{
    private enum Phase { Building, WindPhase, Ended }

    private SceneBuilder sceneBuilder;
    private GridInputHandler inputHandler;
    private BuildSystem buildSystem;
    private GameUIBuilder uiBuilder;
    private PhysicsMergeSystem physicsMerge;
    private DisasterWindController windController;
    private BlockPlacementEffect placementEffect;

    private Phase currentPhase = Phase.Ended;
    private float buildTimer;

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
        // Don't auto-init — GameFlowManager will call InitScene when level is chosen
    }

    private void Update()
    {
        if (currentPhase != Phase.Building) return;

        buildTimer -= Time.deltaTime;
        uiBuilder?.UpdateTimer(buildTimer);

        if (buildTimer <= 0f)
        {
            StartWindPhase();
        }
    }

    private void StartWindPhase()
    {
        currentPhase = Phase.WindPhase;
        inputHandler?.DisableInput();
        uiBuilder?.SetBuildPhaseActive(false);
        uiBuilder?.SetWindPhase();
        windController?.StartDisasterWind();
        Debug.Log("[GameLauncher] Build phase ended, wind started.");
    }

    public void InitScene()
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
        float buildTime = level != null ? level.buildTime : 30f;

        // 1. Build scene
        sceneBuilder = gameObject.AddComponent<SceneBuilder>();
        sceneBuilder.rows = rows;
        sceneBuilder.cols = cols;
        sceneBuilder.cellSize = cellSize;
        sceneBuilder.groundWidth = groundWidth;
        sceneBuilder.groundHeight = groundHeight;
        sceneBuilder.balloonCount = level != null ? level.humanCount : 1;
        sceneBuilder.levelIndex = level != null ? level.level : 1;
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

        // 7. Placement particle effect
        placementEffect = gameObject.AddComponent<BlockPlacementEffect>();

        uiBuilder.CreateUI(buildSystem, windController);
        uiBuilder.SetLevelText($"第{level?.level ?? 1}关: {level?.levelName ?? "默认"}");
        uiBuilder.UpdateObjectCounts(buildSystem);
        uiBuilder.UpdateTimer(buildTime);
        inputHandler.SetUIBuilder(uiBuilder);

        // Start build phase
        buildTimer = buildTime;
        currentPhase = Phase.Building;

        string levelName = level != null ? level.levelName : "Default";
        Debug.Log($"[GameLauncher] Level: {levelName}, Grid {rows}x{cols}, buildTime={buildTime}s, wind={windStrength}/{windDuration}s");
    }

    /// <summary>
    /// Destroys all game subsystems and the GameWorld. Called when leaving the game page.
    /// Does NOT destroy the GamePanel — that is managed by GameFlowManager.
    /// </summary>
    public void CleanupScene()
    {
        currentPhase = Phase.Ended;

        inputHandler?.DisableInput();
        DestroyImmediate(sceneBuilder);
        DestroyImmediate(physicsMerge);
        DestroyImmediate(buildSystem);
        DestroyImmediate(inputHandler);
        DestroyImmediate(uiBuilder);
        DestroyImmediate(windController);
        DestroyImmediate(placementEffect);

        var oldWorld = GameObject.Find("GameWorld");
        if (oldWorld != null) DestroyImmediate(oldWorld);
    }

    /// <summary>
    /// Called by GameFlowManager to rebuild scene for a new level.
    /// </summary>
    public void ReloadScene()
    {
        CleanupScene();
        InitScene();
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
        {
            currentPhase = Phase.Ended;
            windController?.StopWind();
        }
    }

    private void OnDestroy()
    {
        inputHandler?.DisableInput();
    }
}
