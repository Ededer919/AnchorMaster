using System;
using UnityEngine;

namespace AnchorMaster.Workshop
{
    public enum MaterialKind { Wood, Steel, Stone, Balloon }
    public enum WorkshopTool { Move, Wood, Steel, Stone, GroundAnchor, Reinforce, Erase }

    public static class WorkshopText
    {
        const string Traditional = "體國學習錨風場測試過開關線連點選設計畫塊復氣錢數個隊進還轉調臺後壓強頂簡層標準這裡們為與無讓視標記圖壞懸邊兩時間結構評級斷鎖錯應驗製統實現預報餘額點擊選擇緊鄰設置繪製素材其餘線條基本寶貝氣壓負載頭壞擋風單向經驟敗戰繼續個別雲間風將來滿足難題從種數據節約導覽尋找並繫統計話語區塊單純運轉復制說明存盤處理調整關於麽頭風險勝利無法並且對當前區域寬度高度庫存數量數值設置底部開始復原確定提示錯誤無效點擊地圖簡介名稱持續秒數中途反轉方向工具建造替換起始清除只作用於至少否則勝利必須全部留在安全區材料小於建立目標保存";
        const string Simplified = "体国学习锚风场测试过开关线连点选设计画块复气钱数个队进还转调台后压强顶简层标准这里们为与无让视标记图坏悬边两时间结构评级断锁错应验制统实现预报余额点击选择紧邻设置绘制素材其余线条基本宝贝气压负载头坏挡风单向经骤败战继续个别云间风将来满足难题从种数据节约导览寻找并系统计话语区块单纯运转复制说明存盘处理调整关于么头风险胜利无法并且对当前区域宽度高度库存数量数值设置底部开始复原确定提示错误无效点击地图简介名称持续秒数中途反转方向工具建造替换起始清除只作用于至少否则胜利必须全部留在安全区材料小于建立目标保存";

        public static string Simplify(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            var chars = text.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                int index = Traditional.IndexOf(chars[i]);
                if (index >= 0 && index < Simplified.Length) chars[i] = Simplified[index];
            }
            return new string(chars);
        }
    }

    public static class WorkshopMaterial
    {
        public static float Mass(MaterialKind material)
        {
            switch (material)
            {
                case MaterialKind.Steel: return 3f;
                case MaterialKind.Stone: return 6f;
                case MaterialKind.Balloon: return .35f;
                default: return 1f;
            }
        }

        public static float WindDrag(MaterialKind material)
        {
            switch (material)
            {
                case MaterialKind.Steel: return .8f;
                case MaterialKind.Stone: return .35f;
                case MaterialKind.Balloon: return 1.35f;
                default: return 1f;
            }
        }
    }

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
