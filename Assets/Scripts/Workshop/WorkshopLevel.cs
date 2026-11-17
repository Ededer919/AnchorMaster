using System;
using UnityEngine;

namespace AnchorMaster.Workshop
{
    public enum MaterialKind { Wood, Steel, Stone, Balloon }
    public enum WorkshopTool { Move, Wood, Steel, Stone, GroundAnchor, Reinforce, Erase }

    [Serializable]
    public class BlockPlan
    {
        public Vector2Int cell;
        public MaterialKind material;
        public bool preset;
        public bool grounded;
        public BlockPlan Copy() => (BlockPlan)MemberwiseClone();
    }

    [Serializable]
    public struct Reinforcement
    {
        public Vector2Int a, b;
        public bool Matches(Vector2Int x, Vector2Int y) => (a == x && b == y) || (a == y && b == x);
    }

    [CreateAssetMenu(menuName = "AnchorMaster/Workshop Level")]
    public class WorkshopLevel : ScriptableObject
    {
        public string title;
        [TextArea] public string briefing;
        [TextArea] public string hint;
        public int wood = 4, steel = 2, stone = 1, anchors = 3;
        public float duration = 10, windForce = 10;
        public bool reversingWind;
        public BlockPlan[] startingBlocks;

        public static WorkshopLevel CreateDefault(int index)
        {
            var level = CreateInstance<WorkshopLevel>();
            level.name = "Workshop_" + (index + 1);
            var blocks = new System.Collections.Generic.List<BlockPlan>();
            Action<int, int, MaterialKind> add = (x, y, kind) => blocks.Add(new BlockPlan
                { cell = new Vector2Int(x, y), material = kind, preset = true });
            switch (index)
            {
                case 0:
                    level.title = "第一枚地锚";
                    level.briefing = "把小塔固定在地面上，保住顶端的气球。先试一次不放锚，再比较结果。";
                    level.hint = "地锚固定底层部件；蓝色结构锚可以加固塔身或气球连接。";
                    level.windForce = 8; level.anchors = 3;
                    for (int y = -2; y <= 0; y++) add(0, y, MaterialKind.Wood);
                    add(0, 1, MaterialKind.Balloon);
                    break;
                case 1:
                    level.title = "悬臂的代价";
                    level.briefing = "气球挂在悬臂末端。只钉住底座，细长的连接也可能撕裂。";
                    level.hint = "从底层向外搭一根支撑，让力通过两条路径传回地面。";
                    level.windForce = 11; level.anchors = 3;
                    for (int y = -2; y <= 0; y++) add(-1, y, MaterialKind.Wood);
                    add(0, 0, MaterialKind.Wood); add(1, 0, MaterialKind.Wood);
                    add(2, 0, MaterialKind.Balloon);
                    break;
                case 2:
                    level.title = "两边都要守住";
                    level.briefing = "两个气球必须全部留在安全区。分配锚点，或把两座塔连起来。";
                    level.hint = "用横梁分摊负载，比在两座塔上重复加固更省锚点。";
                    level.windForce = 10; level.anchors = 4; level.steel = 4;
                    foreach (int x in new[] { -2, 2 })
                    {
                        for (int y = -2; y <= -1; y++) add(x, y, MaterialKind.Wood);
                        add(x, 0, MaterialKind.Balloon);
                    }
                    break;
                case 3:
                    level.title = "风向会改变";
                    level.briefing = "中途风向反转。靠单侧挡风或单边支撑，未必能撑到最后。";
                    level.hint = "先看风暴时间线，给两个方向都留一条可靠的受力路径。";
                    level.reversingWind = true; level.windForce = 13; level.anchors = 4;
                    for (int y = -2; y <= 0; y++) add(0, y, MaterialKind.Wood);
                    add(-1, 0, MaterialKind.Wood); add(1, 0, MaterialKind.Wood);
                    add(-1, 1, MaterialKind.Balloon); add(1, 1, MaterialKind.Balloon);
                    break;
                default:
                    level.title = "最后的两枚锚";
                    level.briefing = "只有两枚锚。用材料分担负载，再把锚留给最关键的位置。";
                    level.hint = "石块可以配重，钢块可以提高相邻连接强度；材料并非越多越好。";
                    level.windForce = 15; level.anchors = 2; level.wood = 6; level.steel = 4; level.stone = 2;
                    for (int y = -2; y <= 0; y++) add(0, y, MaterialKind.Steel);
                    add(0, 1, MaterialKind.Balloon);
                    break;
            }
            level.startingBlocks = blocks.ToArray();
            return level;
        }
    }
}
