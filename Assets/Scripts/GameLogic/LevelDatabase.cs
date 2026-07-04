using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "LevelDatabase", menuName = "Anchor Game/Level Database")]
public class LevelDatabase : ScriptableObject
{
    public List<LevelData> levels = new List<LevelData>();

    public LevelData GetLevel(int levelIndex)
    {
        if (levelIndex >= 0 && levelIndex < levels.Count)
        {
            return levels[levelIndex];
        }
        return null;
    }

    public int GetTotalLevels()
    {
        return levels.Count;
    }
}
