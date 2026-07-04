using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Build system: spawns objects by type from UI cards. Staged objects immune to wind.
/// Balloons auto-placed on grid center as protection targets.
/// </summary>
public class BuildSystem : MonoBehaviour
{
    private SceneBuilder sceneBuilder;
    private GridObjectManager objectManager;
    private LevelData level;
    private int[] typeCounts;
    private int spawnSlot;

    private List<GridObject> staged = new List<GridObject>();

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

    public void SpawnSpecificType(ObjectType type)
    {
        int quota = GetQuota(type);
        if (typeCounts[(int)type] >= quota)
        {
            UIManager.Instance?.UpdateStatus($"{ObjectConfig.Get(type).name} 已达上限!");
            return;
        }

        var config = ObjectConfig.Get(type);
        string name = config.name;
        Color color = config.color;

        // Balloons are auto-placed by SceneBuilder, skip if user tries to spawn
        if (type == ObjectType.Balloon)
        {
            UIManager.Instance?.UpdateStatus("气球已自动生成在网格上!");
            return;
        }

        Vector3 startPos = sceneBuilder.GetGridTopLeft();
        Vector3 pos = startPos + new Vector3(spawnSlot * sceneBuilder.cellSize * 1.1f, 0, 0);
        spawnSlot++;

        var obj = sceneBuilder.CreateObject(type, name, color, pos);
        typeCounts[(int)type]++;

        staged.Add(obj);
        obj.receiveWind = false;

        Debug.Log($"[Build] Spawned {name} ({type}), count={typeCounts[(int)type]}/{quota}");
        UIManager.Instance?.UpdateStatus($"{name} 已生成 ({typeCounts[(int)type]}/{quota})");

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
