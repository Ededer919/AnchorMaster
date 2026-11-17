using System.Linq;
using UnityEngine;

namespace AnchorMaster.Workshop
{
    public partial class AnchorPuzzleGame
    {
        Font hudFont;

        void OnGUI()
        {
            if (hudFont == null) hudFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            float sx = Screen.width / 1440f, sy = Screen.height / 900f;
            var oldMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(sx, sy, 1));
            GUI.depth = 0;
            DrawHeader();
            DrawLeftPanel();
            DrawRightPanel();
            DrawFooter();
            if (CurrentPhase == Phase.Result) DrawResult();
            GUI.matrix = oldMatrix;
        }

        void DrawHeader()
        {
            Box(new Rect(0, 0, 1440, 104), new Color(.035f, .065f, .09f));
            Label(new Rect(24, 13, 340, 44), "锚固实验室", 30, new Color(.90f, .96f, .98f), true);
            Label(new Rect(25, 59, 360, 23), "ANCHOR LAB   /   结构防灾挑战", 13, new Color(.50f, .68f, .72f));
            Label(new Rect(390, 15, 650, 34), Level.title, 23, Color.white, true);
            Label(new Rect(391, 54, 660, 27), Level.briefing.Replace("基岩", "地面"), 14, new Color(.68f, .78f, .79f));
            int levelCount = Mathf.Max(1, LevelCount);
            int columns = Mathf.Min(levelCount, 10);
            var labels = Enumerable.Range(0, levelCount).Select(i => (i + 1).ToString("00")).ToArray();
            float gridHeight = 36 * Mathf.CeilToInt(levelCount / (float)columns);
            int selected = GUI.SelectionGrid(new Rect(1075, 31, 285, gridHeight), LevelIndex, labels, columns,
                Style(12, new Color(.82f, .90f, .90f), new Color(.11f, .16f, .19f)));
            if (selected != LevelIndex) Initialize(selected);
            Label(new Rect(1367, 38, 55, 22), "關卡", 12, new Color(.53f, .67f, .70f));
        }

        void DrawLeftPanel()
        {
            Box(new Rect(20, 124, 290, 684), new Color(.055f, .09f, .12f));
            Label(new Rect(39, 141, 250, 25), "材料與錨固", 18, Color.white, true);
            Label(new Rect(40, 172, 245, 25), $"錨點餘量   {Level.anchors - UsedAnchors} / {Level.anchors}", 14,
                UsedAnchors >= Level.anchors ? new Color(1, .63f, .38f) : new Color(.48f, .84f, .82f), true);
            Label(new Rect(40, 204, 238, 34), "點選工具後，在場地中放置或選取。", 12, new Color(.55f, .70f, .72f));

            var rows = new[]
            {
                (WorkshopTool.Move, "移動部件", "拖動自建部件"),
                (WorkshopTool.Wood, "木板", "輕便 · 普通強度"),
                (WorkshopTool.Steel, "鋼梁", "較重 · 強化連接"),
                (WorkshopTool.Stone, "石塊", "配重 · 低迎風面"),
                (WorkshopTool.GroundAnchor, "地錨", "固定底層至地面"),
                (WorkshopTool.Reinforce, "結構錨", "強化兩部件連接"),
                (WorkshopTool.Erase, "回收／拆錨", "點選部件或錨點")
            };
            for (int i = 0; i < rows.Length; i++)
            {
                var row = rows[i]; var rect = new Rect(38, 246 + i * 65, 252, 54);
                if (row.Item1 == WorkshopTool.Wood || row.Item1 == WorkshopTool.Steel || row.Item1 == WorkshopTool.Stone)
                {
                    var kind = row.Item1 == WorkshopTool.Wood ? MaterialKind.Wood : row.Item1 == WorkshopTool.Steel ? MaterialKind.Steel : MaterialKind.Stone;
                    Label(new Rect(rect.xMax - 64, rect.y + 3, 58, 22), $"× {Remaining(kind)}", 14, Remaining(kind) > 0 ? Color.white : new Color(.48f, .53f, .55f), true);
                }
                bool chosen = tool == row.Item1;
                string title = row.Item1 == WorkshopTool.GroundAnchor || row.Item1 == WorkshopTool.Reinforce
                    ? row.Item2 + $"   {Level.anchors - UsedAnchors}/{Level.anchors}" : row.Item2;
                if (GUI.Button(rect, title + "\n" + row.Item3, Style(14, chosen ? Color.white : new Color(.77f, .84f, .84f), chosen ? new Color(.06f, .40f, .42f) : new Color(.11f, .16f, .18f), chosen)))
                { tool = row.Item1; selectedEndpoint = null; }
            }
            if (GUI.Button(new Rect(39, 714, 120, 40), "撤銷  Z", Style(14, Color.white, new Color(.13f, .20f, .22f)))) Undo();
            if (GUI.Button(new Rect(169, 714, 120, 40), "提示", Style(14, Color.white, new Color(.13f, .20f, .22f)))) showHint = !showHint;
            if (showHint)
            {
                Box(new Rect(39, 761, 250, 40), new Color(.09f, .16f, .17f));
                Label(new Rect(48, 766, 234, 32), Level.hint, 11, new Color(.69f, .81f, .79f));
            }
        }

        void DrawRightPanel()
        {
            Box(new Rect(1140, 124, 280, 684), new Color(.055f, .09f, .12f));
            Label(new Rect(1160, 143, 240, 26), "風暴預報", 18, Color.white, true);
            DrawWindArrow(new Rect(1161, 184, 232, 70), Level.reversingWind);
            Label(new Rect(1160, 256, 242, 24), Level.reversingWind ? "前半程 →　後半程 ←" : "持續單向風", 13, new Color(.90f, .69f, .40f), true);
            Label(new Rect(1160, 284, 244, 38), $"風力 {Level.windForce:0}    持續 {Level.duration:0} 秒", 13, new Color(.67f, .76f, .77f));
            Label(new Rect(1160, 322, 244, 23), "風暴進程", 12, new Color(.54f, .67f, .69f));
            Box(new Rect(1161, 348, 232, 4), new Color(.25f, .33f, .34f));
            if (Level.reversingWind) Box(new Rect(1274, 344, 2, 12), new Color(.95f, .71f, .39f));
            Label(new Rect(1160, 376, 240, 27), "連接強度", 14, Color.white, true);
            Legend(new Rect(1160, 407, 240, 20), new Color(.50f, .62f, .65f), "普通連接 · 可能斷裂");
            Legend(new Rect(1160, 433, 240, 20), new Color(.25f, .8f, 1), "結構錨 · 高承載");
            Legend(new Rect(1160, 459, 240, 20), new Color(.7f, .48f, 1), "地錨 · 固定地面");
            Label(new Rect(1160, 506, 240, 25), "本次測試", 14, Color.white, true);
            Label(new Rect(1160, 535, 245, 26), $"斷裂連接   {BrokenConnections}", 14, BrokenConnections == 0 ? new Color(.46f, .83f, .63f) : new Color(1, .55f, .42f));
            Label(new Rect(1160, 570, 242, 75), Feedback, 12, new Color(.69f, .79f, .79f));
            if (CurrentPhase == Phase.Build)
            {
                if (GUI.Button(new Rect(1159, 678, 242, 54), "測試風暴   SPACE", Style(17, Color.white, new Color(.08f, .48f, .47f), true))) StartTrial();
                Label(new Rect(1162, 744, 235, 38), "風暴結束後可回到同一方案繼續調整。", 11, new Color(.50f, .66f, .67f));
            }
            else if (CurrentPhase == Phase.Storm)
            {
                Label(new Rect(1160, 680, 235, 28), $"風暴中   {Mathf.Min(elapsed, Level.duration):0.0} / {Level.duration:0}s", 16, new Color(1, .72f, .42f), true);
                if (GUI.Button(new Rect(1159, 724, 242, 46), "停止測試並調整", Style(14, Color.white, new Color(.35f, .23f, .19f)))) { CurrentPhase = Phase.Result; Finish(false); }
            }
        }

        void DrawFooter()
        {
            Box(new Rect(330, 807, 810, 58), new Color(.055f, .09f, .12f));
            Label(new Rect(350, 817, 774, 36), Feedback, 13, new Color(.74f, .83f, .82f));
            Label(new Rect(24, 866, 900, 24), "ANCHOR LAB    ·    圖示素材沿用專案圖片，其餘物理線條由 Unity 基本圖形繪製", 10, new Color(.32f, .45f, .48f));
            if (GUI.Button(new Rect(1272, 818, 128, 38), "重置方案", Style(12, new Color(.8f, .86f, .85f), new Color(.12f, .17f, .19f)))) ResetBlueprint();
        }

        void DrawResult()
        {
            Box(new Rect(330, 247, 810, 380), new Color(.025f, .05f, .07f, .97f));
            Label(new Rect(390, 285, 690, 55), Won ? "防線守住了" : "這次防線失效", 32, Won ? new Color(.45f, .9f, .69f) : new Color(1, .62f, .42f), true);
            Label(new Rect(392, 352, 680, 90), Feedback, 17, new Color(.82f, .88f, .87f));
            if (Won) Label(new Rect(394, 439, 680, 44), "首次斷裂：" + (firstBreak ?? "無") + $"     錨點 {UsedAnchors}/{Level.anchors}", 14, new Color(.71f, .80f, .79f));
            else Label(new Rect(394, 439, 680, 60), "方案已保留，修改一個受力路徑後再試。", 14, new Color(.71f, .80f, .79f));
            if (GUI.Button(new Rect(393, 520, 270, 54), "保留方案調整", Style(17, Color.white, new Color(.08f, .42f, .43f), true))) ReturnToBuild();
            if (GUI.Button(new Rect(682, 520, 180, 54), "重來此關", Style(15, Color.white, new Color(.13f, .20f, .22f)))) ResetBlueprint();
            if (GUI.Button(new Rect(881, 520, 190, 54), LevelIndex < 4 ? "下一關" : "再挑戰", Style(15, Color.white, new Color(.13f, .20f, .22f)))) Initialize((LevelIndex + 1) % 5);
        }

        void DrawWindArrow(Rect rect, bool reversal)
        {
            Label(new Rect(rect.x, rect.y, rect.width, rect.height), reversal ? "←  風向反轉  →" : "←  持續強風  →", 19, new Color(.95f, .70f, .39f), true);
            Box(new Rect(rect.x + 10, rect.yMax - 8, rect.width - 20, 2), new Color(.35f, .25f, .17f));
        }

        void Legend(Rect rect, Color color, string text)
        {
            Box(new Rect(rect.x, rect.y + 4, 13, 13), color);
            Label(new Rect(rect.x + 22, rect.y, rect.width - 22, 20), text, 12, new Color(.69f, .78f, .78f));
        }

        GUIStyle Style(int size, Color color, Color background, bool bold = false)
        {
            return new GUIStyle(GUI.skin.button) { font = hudFont, fontSize = size, fontStyle = bold ? FontStyle.Bold : FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter, wordWrap = true, normal = { textColor = color, background = SolidTexture(background) },
                hover = { textColor = Color.white, background = SolidTexture(new Color(background.r + .04f, background.g + .05f, background.b + .05f)) },
                active = { textColor = Color.white, background = SolidTexture(background * .8f) } };
        }

        readonly System.Collections.Generic.Dictionary<int, Texture2D> styleTextures = new System.Collections.Generic.Dictionary<int, Texture2D>();
        Texture2D SolidTexture(Color color)
        {
            int key = color.GetHashCode();
            if (styleTextures.TryGetValue(key, out var texture)) return texture;
            texture = new Texture2D(1, 1); texture.SetPixel(0, 0, color); texture.Apply(); styleTextures.Add(key, texture); return texture;
        }

        void Box(Rect rect, Color color)
        {
            var old = GUI.color; GUI.color = color; GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = old;
        }

        void Label(Rect rect, string value, int size, Color color, bool bold = false)
        {
            GUI.Label(rect, value, new GUIStyle(GUI.skin.label) { font = hudFont, fontSize = size, fontStyle = bold ? FontStyle.Bold : FontStyle.Normal,
                wordWrap = true, normal = { textColor = color } });
        }
    }
}
