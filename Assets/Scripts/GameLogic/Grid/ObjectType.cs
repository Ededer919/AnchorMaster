using UnityEngine;

/// <summary>
/// Types of grid objects. 5 types total.
/// </summary>
public enum ObjectType
{
    Plank,          // 0 — basic light building material
    StoneWall,      // 1 — heavy wind-resistant wall
    SteelBeam,      // 2 — medium weight structural beam
    BrickWall,      // 3 — medium weight wall, decent wind resistance
    Balloon         // 4 — protection target, if blown out of grid = fail
}

/// <summary>
/// Static configuration table for all object types.
/// </summary>
public static class ObjectConfig
{
    public struct Config
    {
        public string name;
        public string description;
        public Color color;
        public float mass;
        public float windSensitivity;
        public float gravityScale;
        public float bounciness;
        public float friction;
        public int maxCount;
    }

    public static readonly Config[] Table =
    {
        // 0: Plank
        new Config { name = "木板", description = "轻便建材\n抗风差 易搭建", color = new Color(0.6f, 0.4f, 0.2f, 1f),
            mass = 1f, windSensitivity = 0.6f, gravityScale = 1f, bounciness = 0.1f, friction = 0.4f, maxCount = 15 },
        // 1: StoneWall
        new Config { name = "石墙", description = "沉重坚固\n抗风极佳", color = new Color(0.5f, 0.5f, 0.55f, 1f),
            mass = 5f, windSensitivity = 0.1f, gravityScale = 1f, bounciness = 0f, friction = 0.8f, maxCount = 8 },
        // 2: SteelBeam
        new Config { name = "钢梁", description = "中等重量\n性价比高", color = new Color(0.7f, 0.7f, 0.8f, 1f),
            mass = 3f, windSensitivity = 0.15f, gravityScale = 1f, bounciness = 0f, friction = 0.6f, maxCount = 10 },
        // 3: BrickWall
        new Config { name = "砖墙", description = "中等坚固\n抗风良好", color = new Color(0.7f, 0.35f, 0.25f, 1f),
            mass = 3.5f, windSensitivity = 0.2f, gravityScale = 1f, bounciness = 0f, friction = 0.7f, maxCount = 6 },
        // 4: Balloon (protection target)
        new Config { name = "气球", description = "保护目标\n飞出网格即失败", color = new Color(0.95f, 0.6f, 0.1f, 1f),
            mass = 0.3f, windSensitivity = 1f, gravityScale = -0.3f, bounciness = 0f, friction = 0.3f, maxCount = 3 },
    };

    public static Config Get(ObjectType type)
    {
        return Table[(int)type];
    }

    public static readonly Color[] BalloonColors =
    {
        new Color(0.95f, 0.6f, 0.1f),
        new Color(0.95f, 0.3f, 0.3f),
        new Color(0.3f, 0.6f, 0.95f),
    };

    public static readonly string[] BalloonNames =
    {
        "气球A", "气球B", "气球C"
    };
}
