using UnityEngine;

/// <summary>
/// ScriptableObject asset for a single level's configuration.
/// Create via: Assets > Create > GameLogic > Level Data
/// </summary>
[CreateAssetMenu(fileName = "Level_", menuName = "GameLogic/Level Data")]
public class LevelData : ScriptableObject
{
    [Header("Basic")]
    public int level = 1;
    public string levelName = "Level 1";

    [Header("Grid")]
    public int rows = 5;
    public int cols = 5;
    public float cellSize = 1f;

    [Header("Ground")]
    public float groundWidth = 30f;
    public float groundHeight = 4f;

    [Header("Wind")]
    public float windStrength = 3f;
    public float naturalStrength = 1f;
    public float windDuration = 8f;
    public Vector2 windDirection = new Vector2(1f, 0f);

    [Header("Humans")]
    public int humanCount = 1;

    [Header("Object Quotas")]
    public ObjectQuota[] quotas;

    [System.Serializable]
    public struct ObjectQuota
    {
        public ObjectType type;
        public int count;
    }

    /// <summary>
    /// Returns the max count for a given object type in this level.
    /// 0 if not in quotas.
    /// </summary>
    public int GetQuota(ObjectType type)
    {
        if (quotas == null) return 0;
        foreach (var q in quotas)
        {
            if (q.type == type)
                return q.count;
        }
        return 0;
    }
}
