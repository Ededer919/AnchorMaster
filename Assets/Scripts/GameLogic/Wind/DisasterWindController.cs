using UnityEngine;

/// <summary>
/// Controls disaster wind: start/stop, timer, real-time balloon check, result screen.
/// </summary>
public class DisasterWindController : MonoBehaviour
{
    [Header("Wind Settings")]
    public float disasterWindStrength = 5f;
    public float disasterNaturalStrength = 2f;
    public float disasterDuration = 10f;

    private WindSystem2D windSystem;
    private PhysicsMergeSystem physicsMerge;
    private GameUIBuilder uiBuilder;
    private float groundTopY;
    private Rect gridBounds;

    public bool IsWindActive { get; private set; }
    private float windTimer;
    private bool resultShown;

    public void Initialize(WindSystem2D wind, PhysicsMergeSystem physics, GameUIBuilder ui, float groundY, Rect bounds)
    {
        windSystem = wind;
        physicsMerge = physics;
        uiBuilder = ui;
        groundTopY = groundY;
        gridBounds = bounds;
    }

    private void Update()
    {
        if (!IsWindActive) return;

        windTimer -= Time.deltaTime;

        // Real-time check: if any balloon leaves grid bounds → immediate fail
        if (!resultShown && CheckBalloonOutOfGrid())
        {
            TriggerFail();
            return;
        }

        if (windTimer <= 0f)
            StopWind();
    }

    /// <summary>
    /// Returns true if all balloons have been blown out of grid bounds.
    /// </summary>
    private bool CheckBalloonOutOfGrid()
    {
        var allObjs = UnityEngine.Object.FindObjectsByType<GridObject>(UnityEngine.FindObjectsSortMode.None);
        bool anyBalloon = false;

        foreach (var obj in allObjs)
        {
            if (obj == null || obj.objectType != ObjectType.Balloon) continue;
            anyBalloon = true;

            Vector3 pos = obj.transform.position;
            if (gridBounds.Contains(pos))
                return false; // At least one balloon still in bounds
        }

        // No balloons at all = fail; all balloons out of bounds = fail
        return anyBalloon;
    }

    public void StartDisasterWind()
    {
        if (IsWindActive) return;

        IsWindActive = true;
        resultShown = false;
        windTimer = disasterDuration;

        windSystem.globalWindStrength = disasterWindStrength;
        windSystem.naturalWindStrength = disasterNaturalStrength;

        physicsMerge.MergeIntoPhysics();

        EventSystem.Instance.Publish(new WindChangedEvent
        {
            WindDirection = windSystem.globalWindDirection,
            WindStrength = disasterWindStrength
        });

        UIManager.Instance?.UpdateStatus("暴风来袭! 保护气球!");
        uiBuilder?.UpdateWindButtonLabel(true);
        Debug.Log($"[Wind] Disaster wind started for {disasterDuration}s.");
    }

    public void StopWind()
    {
        if (!IsWindActive) return;

        IsWindActive = false;
        windSystem.globalWindStrength = 0f;
        windSystem.naturalWindStrength = 0f;

        physicsMerge.DissolvePhysics();

        EventSystem.Instance.Publish(new WindChangedEvent
        {
            WindDirection = Vector2.zero,
            WindStrength = 0f
        });

        uiBuilder?.UpdateWindButtonLabel(false);

        if (resultShown) return; // Already showed result

        // Wind ended naturally — check if balloons survived
        bool survived = physicsMerge.CheckBalloonSurvival(groundTopY);
        if (survived)
            TriggerWin();
        else
            TriggerFail();
    }

    private void TriggerWin()
    {
        resultShown = true;
        IsWindActive = false;
        windSystem.globalWindStrength = 0f;
        windSystem.naturalWindStrength = 0f;
        physicsMerge.DissolvePhysics();

        UIManager.Instance?.UpdateStatus("气球保住了! 胜利!");
        uiBuilder?.ShowResultScreen(true, "气球存活! 通关!");
        Debug.Log("[Wind] Victory — balloons survived.");
    }

    private void TriggerFail()
    {
        resultShown = true;
        IsWindActive = false;
        windSystem.globalWindStrength = 0f;
        windSystem.naturalWindStrength = 0f;
        physicsMerge.DissolvePhysics();

        UIManager.Instance?.UpdateStatus("气球飞走了! 失败!");
        uiBuilder?.ShowResultScreen(false, "气球被吹出网格! 锚固失败!");
        Debug.Log("[Wind] Failed — balloon blown out of grid.");
    }

    public void ToggleWind()
    {
        if (IsWindActive)
            StopWind();
        else
            StartDisasterWind();
    }
}
