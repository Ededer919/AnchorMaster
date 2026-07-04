using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Build system: drag-from-toolbar spawning. Clicking a card starts dragging a new object.
/// Quota consumed on placement; cancelled drags return the quota.
/// </summary>
public class BuildSystem : MonoBehaviour
{
    private SceneBuilder sceneBuilder;
    private GridObjectManager objectManager;
    private LevelData level;
    private int[] typeCounts;
    private int spawnSlot;

    private List<GridObject> staged = new List<GridObject>();

    // Pending drag: object created but not yet placed
    private GridObject pendingDrag;
    private ObjectType pendingType;

    public bool IsBuildMode { get; private set; }

    public void Initialize(SceneBuilder scene, GridObjectManager manager, LevelData levelData)
    {
        sceneBuilder = scene;
        objectManager = manager;
        level = levelData;
        typeCounts = new int[System.Enum.GetValues(typeof(ObjectType)).Length];
    }

    public void ToggleMode()
    {
        IsBuildMode = !IsBuildMode;
        string modeName = IsBuildMode ? "自由模式" : "网格模式";
        UIManager.Instance?.UpdateStatus($"模式切换: {modeName}");
    }

    public bool IsStaged(GridObject obj)
    {
        return staged.Contains(obj);
    }

    public int GetQuota(ObjectType type)
    {
        return level != null ? level.GetQuota(type) : ObjectConfig.Get(type).maxCount;
    }

    public int GetCount(ObjectType type)
    {
        return typeCounts[(int)type];
    }

    /// <summary>
    /// Returns the remaining available count for a type (quota - used).
    /// </summary>
    public int GetAvailable(ObjectType type)
    {
        return GetQuota(type) - GetCount(type);
    }

    /// <summary>
    /// Called when user clicks a card in the toolbar. Creates a new object at mouse position
    /// and starts dragging it. Returns the object or null if quota exhausted.
    /// </summary>
    public GridObject BeginDragFromToolbar(ObjectType type, Vector3 worldPos)
    {
        if (type == ObjectType.Balloon)
        {
            UIManager.Instance?.UpdateStatus("气球已自动生成在网格上!");
            return null;
        }

        int available = GetAvailable(type);
        if (available <= 0)
        {
            UIManager.Instance?.UpdateStatus($"{ObjectConfig.Get(type).name} 已用完!");
            return null;
        }

        var config = ObjectConfig.Get(type);
        string name = config.name;

        // Create object at mouse position
        var obj = sceneBuilder.CreateObject(type, name, config.color, worldPos);
        obj.IsDragging = true;
        obj.receiveWind = false;
        staged.Add(obj);

        pendingDrag = obj;
        pendingType = type;

        // Consume quota immediately (will restore on cancel)
        typeCounts[(int)type]++;

        var ui = FindFirstObjectByType<GameUIBuilder>();
        if (ui != null) ui.UpdateObjectCounts(this);

        Debug.Log($"[Build] Drag started: {name} ({type}), remaining: {GetAvailable(type)}");
        return obj;
    }

    /// <summary>
    /// Called when a toolbar-dragged object is successfully placed on grid.
    /// </summary>
    public void ConfirmDragPlacement(GridObject obj)
    {
        if (staged.Contains(obj))
            staged.Remove(obj);
        obj.receiveWind = true;

        pendingDrag = null;

        var ui = FindFirstObjectByType<GameUIBuilder>();
        if (ui != null) ui.UpdateObjectCounts(this);

        Debug.Log($"[Build] Placed: {obj.displayName} ({obj.objectType}), remaining: {GetAvailable(obj.objectType)}");
    }

    /// <summary>
    /// Called when a toolbar-dragged object is cancelled (not placed).
    /// Restores the quota and destroys the object.
    /// </summary>
    public void CancelDrag(GridObject obj)
    {
        if (obj == null) return;

        // Restore quota
        typeCounts[(int)obj.objectType]--;

        if (staged.Contains(obj))
            staged.Remove(obj);

        if (obj == pendingDrag)
            pendingDrag = null;

        Destroy(obj.gameObject);

        var ui = FindFirstObjectByType<GameUIBuilder>();
        if (ui != null) ui.UpdateObjectCounts(this);

        Debug.Log($"[Build] Drag cancelled: {obj.displayName} ({obj.objectType}), restored: {GetAvailable(obj.objectType)}");
    }

    /// <summary>
    /// Spawns an object at the nearest empty cell to the balloon(s).
    /// Called when player clicks a toolbar card.
    /// </summary>
    public bool SpawnNearBalloon(ObjectType type)
    {
        if (type == ObjectType.Balloon)
        {
            UIManager.Instance?.UpdateStatus("气球已自动生成在网格上!");
            return false;
        }

        int available = GetAvailable(type);
        if (available <= 0)
        {
            UIManager.Instance?.UpdateStatus($"{ObjectConfig.Get(type).name} 已用完!");
            return false;
        }

        // Find balloon positions
        var grid = sceneBuilder.GridSystem;
        var objs = sceneBuilder.ObjectManager.GetAllObjects();
        Vector2 balloonCenter = Vector2.zero;
        int balloonCount = 0;
        foreach (var go in objs)
        {
            if (go != null && go.objectType == ObjectType.Balloon)
            {
                balloonCenter += (Vector2)go.transform.position;
                balloonCount++;
            }
        }

        if (balloonCount == 0)
        {
            UIManager.Instance?.UpdateStatus("未找到气球!");
            return false;
        }

        balloonCenter /= balloonCount;

        // Search for nearest empty cell to balloon center
        Vector2Int bestCell = new Vector2Int(-1, -1);
        float minDist = float.MaxValue;

        for (int r = 0; r < grid.Rows; r++)
        {
            for (int c = 0; c < grid.Cols; c++)
            {
                if (!grid.CanPlaceAt(r, c)) continue;

                var cell = grid.GetCell(r, c);
                if (cell == null) continue;

                float dist = Vector2.Distance(balloonCenter, cell.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    bestCell = new Vector2Int(c, r);
                }
            }
        }

        if (bestCell.x < 0)
        {
            UIManager.Instance?.UpdateStatus("气球周围已无空位!");
            return false;
        }

        // Create and place the object
        var config = ObjectConfig.Get(type);
        Vector3 worldPos = grid.GridToWorldPosition(bestCell.y, bestCell.x);
        var newObj = sceneBuilder.CreateObject(type, config.name, config.color, worldPos);
        newObj.PlaceOnGrid(grid, bestCell.y, bestCell.x);
        newObj.receiveWind = false;

        typeCounts[(int)type]++;

        var ui = FindFirstObjectByType<GameUIBuilder>();
        if (ui != null) ui.UpdateObjectCounts(this);

        UIManager.Instance?.UpdateStatus($"{config.name} 已放置在 ({bestCell.y},{bestCell.x})");
        Debug.Log($"[Build] SpawnNearBalloon: {config.name} at ({bestCell.y},{bestCell.x}), remaining: {GetAvailable(type)}");
        return true;
    }

    // Legacy support for SpawnSpecificType (used by old UI)
    public void SpawnSpecificType(ObjectType type)
    {
        if (type == ObjectType.Balloon)
        {
            UIManager.Instance?.UpdateStatus("气球已自动生成在网格上!");
            return;
        }

        int available = GetAvailable(type);
        if (available <= 0)
        {
            UIManager.Instance?.UpdateStatus($"{ObjectConfig.Get(type).name} 已用完!");
            return;
        }

        var config = ObjectConfig.Get(type);
        Vector3 startPos = sceneBuilder.GetGridTopLeft();
        Vector3 pos = startPos + new Vector3(spawnSlot * sceneBuilder.cellSize * 1.1f, 0, 0);
        spawnSlot++;

        var obj = sceneBuilder.CreateObject(type, config.name, config.color, pos);
        typeCounts[(int)type]++;
        staged.Add(obj);
        obj.receiveWind = false;

        Debug.Log($"[Build] Spawned {config.name} ({type}), count={typeCounts[(int)type]}/{GetQuota(type)}");

        var ui = FindFirstObjectByType<GameUIBuilder>();
        if (ui != null) ui.UpdateObjectCounts(this);
    }

    public void OnObjectPickedUp(GridObject obj)
    {
        if (staged.Contains(obj))
            staged.Remove(obj);
    }

    public void OnObjectPlaced(GridObject obj)
    {
        if (staged.Contains(obj))
            staged.Remove(obj);
        obj.receiveWind = true;
    }
}
