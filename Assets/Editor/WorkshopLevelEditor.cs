using System.Collections.Generic;
using System.Linq;
using AnchorMaster.Workshop;
using UnityEditor;
using UnityEngine;

public sealed class WorkshopLevelEditor : EditorWindow
{
    enum BoardTool { Wood, Steel, Stone, Balloon, Erase, ToggleGroundAnchor }

    const string LevelFolder = "Assets/Resources/WorkshopLevels";
    readonly List<WorkshopLevel> levels = new List<WorkshopLevel>();
    WorkshopLevel current;
    BoardTool boardTool;
    Vector2 scroll;

    [MenuItem("AnchorMaster/关卡制作工具", false, 20)]
    static void Open() => GetWindow<WorkshopLevelEditor>("关卡制作");

    void OnEnable() => RefreshLevels();

    void RefreshLevels()
    {
        levels.Clear();
        foreach (string guid in AssetDatabase.FindAssets("t:WorkshopLevel", new[] { LevelFolder }))
        {
            var level = AssetDatabase.LoadAssetAtPath<WorkshopLevel>(AssetDatabase.GUIDToAssetPath(guid));
            if (level != null) levels.Add(level);
        }
        levels.Sort((a, b) =>
        {
            int order = LevelSortNumber(a.name).CompareTo(LevelSortNumber(b.name));
            return order != 0 ? order : string.CompareOrdinal(a.name, b.name);
        });
        if (current == null || !levels.Contains(current)) current = levels.FirstOrDefault();
    }

    void OnGUI()
    {
        DrawToolbar();
        if (current == null)
        {
            EditorGUILayout.HelpBox("尚无关卡资源，创建一个新关卡开始编辑。", MessageType.Info);
            return;
        }

        scroll = EditorGUILayout.BeginScrollView(scroll);
        DrawLevelSettings();
        EditorGUILayout.Space(8);
        DrawBoard();
        EditorGUILayout.Space(8);
        DrawValidation();
        EditorGUILayout.EndScrollView();
    }

    void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            var choices = levels.Select(level => $"{level.name}  |  {WorkshopText.Simplify(level.title)}").ToArray();
            int selected = current == null ? -1 : levels.IndexOf(current);
            int next = EditorGUILayout.Popup(selected, choices, EditorStyles.toolbarPopup, GUILayout.MinWidth(190));
            if (next >= 0 && next != selected) current = levels[next];

            if (GUILayout.Button("新建", EditorStyles.toolbarButton, GUILayout.Width(48))) CreateLevel(null);
            using (new EditorGUI.DisabledScope(current == null))
            {
                if (GUILayout.Button("复制", EditorStyles.toolbarButton, GUILayout.Width(48))) CreateLevel(current);
                if (GUILayout.Button("保存", EditorStyles.toolbarButton, GUILayout.Width(48))) SaveCurrent();
                if (GUILayout.Button("定位", EditorStyles.toolbarButton, GUILayout.Width(48)))
                {
                    Selection.activeObject = current;
                    EditorGUIUtility.PingObject(current);
                }
            }
        }
    }

    void DrawLevelSettings()
    {
        current.title = WorkshopText.Simplify(current.title);
        current.briefing = WorkshopText.Simplify(current.briefing);
        current.hint = WorkshopText.Simplify(current.hint);
        var serializedObject = new SerializedObject(current);
        serializedObject.Update();
        EditorGUILayout.LabelField("关卡数据", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("title"), new GUIContent("名称"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("briefing"), new GUIContent("关卡简介"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("hint"), new GUIContent("提示"));
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("风暴设置", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("windForce"), new GUIContent("风力"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("duration"), new GUIContent("持续秒数"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("reversingWind"), new GUIContent("中途反转风向"));
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("玩家可用材料", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("wood"), new GUIContent("木板"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("steel"), new GUIContent("钢梁"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("stone"), new GUIContent("石块"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("anchors"), new GUIContent("锚点"));
        }
        if (serializedObject.ApplyModifiedProperties()) EditorUtility.SetDirty(current);
    }

    void DrawBoard()
    {
        EditorGUILayout.LabelField("起始地图", EditorStyles.boldLabel);
        boardTool = (BoardTool)GUILayout.Toolbar((int)boardTool,
            new[] { "木板", "钢梁", "石块", "气球", "橡皮擦", "地锚" });
        EditorGUILayout.HelpBox("点击格子放置或替换起始部件；选择橡皮擦清除。地锚工具只作用于最底排。", MessageType.None);
        for (int y = 3; y >= -2; y--)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(y.ToString(), GUILayout.Width(20), GUILayout.Height(38));
                for (int x = -5; x <= 5; x++) DrawCell(x, y);
            }
        }
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Space(20);
            for (int x = -5; x <= 5; x++) GUILayout.Label(x.ToString(), GUILayout.Width(44));
        }
    }

    void DrawCell(int x, int y)
    {
        var cell = new Vector2Int(x, y);
        var block = current.startingBlocks?.FirstOrDefault(item => item.cell == cell);
        string label = block == null ? "·" : MaterialLabel(block.material) + (block.grounded ? "\n⚓" : "");
        Color old = GUI.backgroundColor;
        GUI.backgroundColor = block == null ? new Color(.20f, .23f, .25f) : MaterialColor(block.material);
        if (GUILayout.Button(label, GUILayout.Width(44), GUILayout.Height(cellSize))) EditCell(cell, block);
        GUI.backgroundColor = old;
    }

    void EditCell(Vector2Int cell, BlockPlan block)
    {
        Undo.RecordObject(current, "Edit workshop starting layout");
        var blocks = current.startingBlocks?.Select(item => item.Copy()).ToList() ?? new List<BlockPlan>();
        int index = blocks.FindIndex(item => item.cell == cell);

        if (boardTool == BoardTool.Erase)
        {
            if (index >= 0) blocks.RemoveAt(index);
        }
        else if (boardTool == BoardTool.ToggleGroundAnchor)
        {
            if (index >= 0 && cell.y == -2 && blocks[index].material != MaterialKind.Balloon)
                blocks[index].grounded = !blocks[index].grounded;
            else if (cell.y != -2)
                ShowNotification(new GUIContent("地锚只能设在最底排"));
        }
        else
        {
            var material = boardTool == BoardTool.Wood ? MaterialKind.Wood
                : boardTool == BoardTool.Steel ? MaterialKind.Steel
                : boardTool == BoardTool.Stone ? MaterialKind.Stone : MaterialKind.Balloon;
            if (index < 0) blocks.Add(new BlockPlan { cell = cell, material = material, preset = true });
            else
            {
                blocks[index].material = material;
                blocks[index].preset = true;
                if (material == MaterialKind.Balloon) blocks[index].grounded = false;
            }
        }

        current.startingBlocks = blocks.ToArray();
        EditorUtility.SetDirty(current);
    }

    void DrawValidation()
    {
        var blocks = current.startingBlocks ?? new BlockPlan[0];
        var duplicates = blocks.GroupBy(block => block.cell).Where(group => group.Count() > 1).Count();
        int balloons = blocks.Count(block => block.material == MaterialKind.Balloon);
        EditorGUILayout.LabelField("检查", EditorStyles.boldLabel);
        if (duplicates > 0) EditorGUILayout.HelpBox("存在重复占用的格子。", MessageType.Error);
        if (balloons == 0) EditorGUILayout.HelpBox("至少放置一个气球，否则关卡无法获胜。", MessageType.Error);
        else if (balloons > 1) EditorGUILayout.HelpBox($"起始气球：{balloons} 个，必须全部留在安全区。", MessageType.Info);
        if (blocks.Any(block => block.grounded && (block.cell.y != -2 || block.material == MaterialKind.Balloon)))
            EditorGUILayout.HelpBox("地锚只能放在底排非气球部件上。", MessageType.Error);
        if (current.windForce <= 0 || current.duration <= 0)
            EditorGUILayout.HelpBox("风力和持续时间必须大于 0。", MessageType.Error);
        if (current.wood < 0 || current.steel < 0 || current.stone < 0 || current.anchors < 0)
            EditorGUILayout.HelpBox("材料和锚点数量不能小于 0。", MessageType.Error);
        if (GUILayout.Button("保存并检查")) SaveCurrent();
    }

    void CreateLevel(WorkshopLevel source)
    {
        if (!AssetDatabase.IsValidFolder(LevelFolder))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
            AssetDatabase.CreateFolder("Assets/Resources", "WorkshopLevels");
        }

        int number = 1;
        while (AssetDatabase.LoadAssetAtPath<WorkshopLevel>($"{LevelFolder}/Workshop_{number}.asset") != null) number++;
        string path = AssetDatabase.GenerateUniqueAssetPath($"{LevelFolder}/Workshop_{number}.asset");
        WorkshopLevel created;
        if (source != null)
        {
            created = Instantiate(source);
            created.startingBlocks = source.startingBlocks?.Select(item => item.Copy()).ToArray();
            created.title = source.title + "（副本）";
        }
        else
        {
            created = WorkshopLevel.CreateDefault(0);
            created.title = "新关卡 " + number;
            created.briefing = "设计你的关卡目标。";
            created.hint = "";
        }
        created.name = $"Workshop_{number}";
        AssetDatabase.CreateAsset(created, path);
        AssetDatabase.SaveAssets();
        RefreshLevels();
        current = created;
        Selection.activeObject = current;
    }

    void SaveCurrent()
    {
        if (current == null) return;
        var blocks = current.startingBlocks ?? new BlockPlan[0];
        bool invalid = blocks.GroupBy(block => block.cell).Any(group => group.Count() > 1)
            || blocks.Any(block => block.cell.x < -5 || block.cell.x > 5 || block.cell.y < -2 || block.cell.y > 3)
            || blocks.Any(block => block.grounded && (block.cell.y != -2 || block.material == MaterialKind.Balloon))
            || !blocks.Any(block => block.material == MaterialKind.Balloon)
            || current.windForce <= 0 || current.duration <= 0
            || current.wood < 0 || current.steel < 0 || current.stone < 0 || current.anchors < 0;
        if (invalid)
        {
            EditorUtility.DisplayDialog("无法保存", "请先修正地图格子、气球、地锚或风暴参数错误。", "确定");
            return;
        }
        EditorUtility.SetDirty(current);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        ShowNotification(new GUIContent("关卡已保存"));
    }

    static string MaterialLabel(MaterialKind material) => material == MaterialKind.Wood ? "木" : material == MaterialKind.Steel ? "钢" : material == MaterialKind.Stone ? "石" : "球";

    static int LevelSortNumber(string levelName)
    {
        var suffix = levelName.StartsWith("Workshop_") ? levelName.Substring("Workshop_".Length) : levelName;
        return int.TryParse(suffix, out int number) ? number : int.MaxValue;
    }

    static Color MaterialColor(MaterialKind material) => material == MaterialKind.Wood ? new Color(.72f, .46f, .24f)
        : material == MaterialKind.Steel ? new Color(.40f, .66f, .73f)
        : material == MaterialKind.Stone ? new Color(.55f, .59f, .58f) : new Color(.94f, .65f, .30f);
}
