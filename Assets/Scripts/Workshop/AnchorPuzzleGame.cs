using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AnchorMaster.Workshop
{
    /// <summary>Blueprint editing is independent from disposable physics trials.</summary>
    public partial class AnchorPuzzleGame : MonoBehaviour
    {
        public enum Phase { Build, Storm, Result }
        public Phase CurrentPhase { get; private set; }
        public int LevelIndex { get; private set; }
        public int LevelCount { get; private set; }
        public WorkshopLevel Level { get; private set; }
        public IReadOnlyList<BlockPlan> Blueprint => plan;
        public int BrokenConnections { get; private set; }
        public bool Won { get; private set; }
        public string Feedback { get; private set; }

        readonly List<BlockPlan> plan = new List<BlockPlan>();
        readonly List<Reinforcement> reinforcements = new List<Reinforcement>();
        readonly Stack<Snapshot> history = new Stack<Snapshot>();
        readonly Dictionary<Vector2Int, BodyView> bodies = new Dictionary<Vector2Int, BodyView>();
        readonly List<LinkView> links = new List<LinkView>();
        readonly List<Object> ownedAssets = new List<Object>();
        GameObject world;
        Camera viewCamera;
        Rect previousCameraRect;
        Vector3 previousCameraPosition;
        float previousCameraSize;
        Color previousCameraColor;
        int previousCameraMask;
        Sprite square, circle;
        Material lineMaterial;
        WorkshopTool tool;
        Vector2Int? draggedCell, selectedEndpoint;
        float elapsed, shake;
        bool initialized, showHint, ownsDefaultLevel;
        string firstBreak;

        class Snapshot
        {
            public List<BlockPlan> blocks;
            public List<Reinforcement> connections;
        }
        class BodyView
        {
            public BlockPlan plan;
            public Rigidbody2D rb;
            public SpriteRenderer sprite;
        }
        class LinkView
        {
            public BodyView a, b;
            public FixedJoint2D joint;
            public LineRenderer line;
            public bool reinforced, ground, broken;
            public float stress;
        }

        void Start() { if (!initialized) Initialize(0); }

        public void Initialize(int index)
        {
            if (!initialized)
            {
                initialized = true;
                viewCamera = Camera.main;
                if (viewCamera == null)
                {
                    var cameraObject = new GameObject("Workshop Camera", typeof(Camera), typeof(AudioListener));
                    cameraObject.transform.SetParent(transform);
                    viewCamera = cameraObject.GetComponent<Camera>();
                    viewCamera.tag = "MainCamera";
                }
                previousCameraRect = viewCamera.rect;
                previousCameraPosition = viewCamera.transform.position;
                previousCameraSize = viewCamera.orthographicSize;
                previousCameraColor = viewCamera.backgroundColor;
                previousCameraMask = viewCamera.cullingMask;
                square = MakeSprite(false); circle = MakeSprite(true);
                lineMaterial = new Material(Shader.Find("Sprites/Default"));
                ownedAssets.Add(lineMaterial);
            }
            var availableLevels = Resources.LoadAll<WorkshopLevel>("WorkshopLevels")
                .OrderBy(level => LevelSortNumber(level.name))
                .ThenBy(level => level.name, System.StringComparer.Ordinal).ToArray();
            LevelCount = availableLevels.Length;
            LevelIndex = Mathf.Clamp(index, 0, Mathf.Max(0, availableLevels.Length - 1));
            if (ownsDefaultLevel) DestroyOwned(Level);
            Level = availableLevels.Length > 0 ? availableLevels[LevelIndex] : null;
            ownsDefaultLevel = Level == null;
            if (ownsDefaultLevel) Level = WorkshopLevel.CreateDefault(Mathf.Min(LevelIndex, 4));
            ResetBlueprint();
        }

        static int LevelSortNumber(string levelName)
        {
            var suffix = levelName.StartsWith("Workshop_", System.StringComparison.Ordinal)
                ? levelName.Substring("Workshop_".Length) : levelName;
            return int.TryParse(suffix, out int number) ? number : int.MaxValue;
        }

        public void ResetBlueprint()
        {
            plan.Clear();
            plan.AddRange(Level.startingBlocks.Select(b => b.Copy()));
            reinforcements.Clear(); history.Clear();
            ReturnToBuild();
        }

        public void ReturnToBuild()
        {
            CurrentPhase = Phase.Build; elapsed = 0; shake = 0;
            draggedCell = null; selectedEndpoint = null; firstBreak = null;
            Feedback = "观察风向 → 搭建支撑 → 放置锚点 → 点击测试风暴。";
            RebuildWorld();
        }

        void SaveUndo()
        {
            history.Push(new Snapshot { blocks = plan.Select(b => b.Copy()).ToList(), connections = new List<Reinforcement>(reinforcements) });
        }

        public void Undo()
        {
            if (CurrentPhase != Phase.Build || history.Count == 0) return;
            var snapshot = history.Pop();
            plan.Clear(); plan.AddRange(snapshot.blocks);
            reinforcements.Clear(); reinforcements.AddRange(snapshot.connections);
            selectedEndpoint = null; draggedCell = null;
            RebuildWorld(); Feedback = "已撤销上一步，材料与锚点已退还。";
        }

        public int UsedAnchors => plan.Count(b => b.grounded) + reinforcements.Count;
        public int Remaining(MaterialKind kind)
        {
            int quota = kind == MaterialKind.Wood ? Level.wood : kind == MaterialKind.Steel ? Level.steel : Level.stone;
            return quota - plan.Count(b => !b.preset && b.material == kind);
        }
        public bool CanPlace(Vector2Int cell) => cell.x >= -5 && cell.x <= 5 && cell.y >= -2 && cell.y <= 3 && plan.All(b => b.cell != cell);

        public bool Place(MaterialKind material, Vector2Int cell)
        {
            if (CurrentPhase != Phase.Build || material == MaterialKind.Balloon || Remaining(material) <= 0 || !CanPlace(cell)) return false;
            SaveUndo(); plan.Add(new BlockPlan { cell = cell, material = material });
            RebuildWorld(); Feedback = "已放置。相邻部件自动连接，灰线是可断裂的普通连接。";
            AudioManager.Instance?.PlayPut(); return true;
        }

        public bool SetGroundAnchor(Vector2Int cell)
        {
            if (CurrentPhase != Phase.Build) return false;
            var block = plan.Find(b => b.cell == cell);
            if (block == null || block.material == MaterialKind.Balloon || cell.y != -2)
            { Feedback = "地锚只能固定最底层、紧邻地面的部件。"; return false; }
            if (!block.grounded && UsedAnchors >= Level.anchors) { Feedback = "锚点已用完，可撤销或移除已有锚点。"; return false; }
            SaveUndo(); block.grounded = !block.grounded; RebuildWorld();
            Feedback = block.grounded ? "已固定到地面。塔身仍可能从普通连接处断裂。" : "地锚已回收。";
            return true;
        }

        public bool Reinforce(Vector2Int a, Vector2Int b)
        {
            if (CurrentPhase != Phase.Build || !Adjacent(a, b) || !bodies.ContainsKey(a) || !bodies.ContainsKey(b))
            { Feedback = "请选择两个上下或左右相邻的部件。"; return false; }
            int existing = reinforcements.FindIndex(r => r.Matches(a, b));
            if (existing < 0 && UsedAnchors >= Level.anchors) { Feedback = "锚点已用完。"; return false; }
            SaveUndo();
            if (existing >= 0) reinforcements.RemoveAt(existing);
            else reinforcements.Add(new Reinforcement { a = a, b = b });
            RebuildWorld(); Feedback = existing < 0 ? "蓝色结构锚已加固这条连接。" : "结构锚已回收。";
            return true;
        }

        public bool MoveBlock(Vector2Int from, Vector2Int to)
        {
            var block = plan.Find(b => b.cell == from);
            if (CurrentPhase != Phase.Build || block == null || block.preset || !CanPlace(to)) return false;
            SaveUndo(); RemoveAnchorsAt(from); block.cell = to; RebuildWorld();
            Feedback = "部件已移动，原位置的锚点已回收。"; return true;
        }

        public bool Erase(Vector2Int cell)
        {
            var block = plan.Find(b => b.cell == cell);
            if (CurrentPhase != Phase.Build || block == null) return false;
            if (block.preset && !block.grounded && !reinforcements.Any(r => r.a == cell || r.b == cell))
            { Feedback = "关卡自带部件不能删除；可以移除上面的锚点。"; return false; }
            SaveUndo(); RemoveAnchorsAt(cell);
            if (!block.preset) plan.Remove(block);
            RebuildWorld(); Feedback = "已回收部件或锚点。"; return true;
        }

        void RemoveAnchorsAt(Vector2Int cell)
        {
            var block = plan.Find(b => b.cell == cell);
            if (block != null) block.grounded = false;
            reinforcements.RemoveAll(r => r.a == cell || r.b == cell);
        }

        public static bool Adjacent(Vector2Int a, Vector2Int b) => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) == 1;

        void Update()
        {
            ConfigureCamera();
            if (placementPreview != null) placementPreview.SetActive(false);
            if (CurrentPhase != Phase.Build) return;
            if (Input.GetKeyDown(KeyCode.Z)) Undo();
            if (Input.GetKeyDown(KeyCode.Space)) StartTrial();
            if (Input.GetKeyDown(KeyCode.Escape)) { draggedCell = null; selectedEndpoint = null; tool = WorkshopTool.Move; }
            var mouse = new Vector2(Input.mousePosition.x / Screen.width * 1440, (1 - Input.mousePosition.y / Screen.height) * 900);
            if (!new Rect(330, 140, 810, 645).Contains(mouse))
            {
                if (Input.GetMouseButtonUp(0) && draggedCell.HasValue) { draggedCell = null; RebuildWorld(); }
                return;
            }
            Vector2 worldPoint = viewCamera.ScreenToWorldPoint(Input.mousePosition);
            var cell = Vector2Int.RoundToInt(worldPoint);
            UpdatePlacementPreview(cell);
            if (Input.GetMouseButtonDown(1)) { Erase(cell); return; }
            if (draggedCell.HasValue && Input.GetMouseButton(0) && bodies.TryGetValue(draggedCell.Value, out var movingBody))
            {
                movingBody.rb.position = cell;
                movingBody.sprite.color = CanPlace(cell) || cell == draggedCell.Value ? Color.white : new Color(1, .4f, .35f);
            }
            if (Input.GetMouseButtonDown(0))
            {
                if (tool == WorkshopTool.Move)
                {
                    var block = plan.Find(b => b.cell == cell);
                    if (block != null && !block.preset) draggedCell = cell;
                    else Feedback = "拖动自己添加的部件；关卡自带部件保持原位。";
                }
                else if (tool == WorkshopTool.GroundAnchor) SetGroundAnchor(cell);
                else if (tool == WorkshopTool.Reinforce)
                {
                    if (!bodies.ContainsKey(cell)) { selectedEndpoint = null; return; }
                    if (selectedEndpoint.HasValue) { Reinforce(selectedEndpoint.Value, cell); selectedEndpoint = null; }
                    else { selectedEndpoint = cell; Feedback = "再点击一个相邻部件，加固它们之间的连接。"; }
                }
                else if (tool == WorkshopTool.Erase) Erase(cell);
                else Place(tool == WorkshopTool.Wood ? MaterialKind.Wood : tool == WorkshopTool.Steel ? MaterialKind.Steel : MaterialKind.Stone, cell);
            }
            if (Input.GetMouseButtonUp(0) && draggedCell.HasValue)
            {
                if (!MoveBlock(draggedCell.Value, cell)) RebuildWorld();
                draggedCell = null;
            }
        }

        public void StartTrial()
        {
            if (CurrentPhase != Phase.Build) return;
            draggedCell = null; selectedEndpoint = null;
            BrokenConnections = 0; firstBreak = null; elapsed = 0; Won = false;
            CurrentPhase = Phase.Storm;
            if (placementPreview != null) placementPreview.SetActive(false);
            foreach (var body in bodies.Values) { body.rb.bodyType = RigidbodyType2D.Dynamic; body.rb.WakeUp(); }
            Feedback = "观察连接：绿色安全 → 橙色吃紧 → 红色即将断裂。";
        }

        void FixedUpdate() { StepTrial(Time.fixedDeltaTime); }

        // Explicit stepping also makes real Unity physics regression tests deterministic.
        public void StepTrial(float dt)
        {
            if (CurrentPhase != Phase.Storm) return;
            elapsed += dt;
            float direction = Level.reversingWind && elapsed > Level.duration * .5f ? -1 : 1;
            float envelope = elapsed < 2 ? Mathf.Lerp(.2f, 1, elapsed / 2) : elapsed > Level.duration - 2 ? .65f : 1;
            float gust = 1 + .2f * Mathf.Sin(elapsed * 4);
            foreach (var body in bodies.Values)
            {
                var position = body.rb.position;
                // Cast from outside the collider, and count only blocks between the wind and this body.
                var hits = Physics2D.RaycastAll(position + Vector2.left * direction * .55f, Vector2.left * direction, 10);
                bool sheltered = hits.Any(h => h.rigidbody != null && h.rigidbody != body.rb && h.collider.gameObject.layer == 8);
                float exposure = sheltered ? .28f : 1;
                body.rb.AddForce(Vector2.right * direction * Level.windForce * envelope * gust * exposure);
            }
            foreach (var link in links)
            {
                if (link.joint != null)
                {
                    float accumulatedLoad = AccumulatedWindLoad(link, direction, envelope * gust);
                    link.stress = Mathf.Max(link.stress,
                        Mathf.Clamp01(accumulatedLoad / link.joint.breakForce));
                    if (accumulatedLoad > link.joint.breakForce)
                    {
                        var failedJoint = link.joint;
                        link.joint = null;
                        failedJoint.enabled = false;
                        Destroy(failedJoint);
                    }
                }
                if (link.joint == null && !link.broken)
                {
                    link.broken = true; BrokenConnections++; shake = .12f;
                    if (firstBreak == null) firstBreak = link.ground ? "地锚过载" : $"({link.a.plan.cell.x},{link.a.plan.cell.y}) 附近的连接先断裂";
                    Feedback = firstBreak + "。方案已保留，可返回调整。";
                }
                if (link.joint != null)
                    link.stress = Mathf.Max(link.stress, Mathf.Clamp01(
                        link.joint.GetReactionForce(1 / dt).magnitude / link.joint.breakForce));
            }
            var targets = bodies.Values.Where(b => b.plan.material == MaterialKind.Balloon).ToList();
            if (targets.Any(b => b.rb.position.y < -2.65f || b.rb.position.y > 3.8f || Mathf.Abs(b.rb.position.x) > 5.8f))
                Finish(false);
            else if (elapsed >= Level.duration) Finish(targets.Count > 0);
        }

        void Finish(bool win)
        {
            Won = win; CurrentPhase = Phase.Result;
            foreach (var body in bodies.Values) { body.rb.velocity = Vector2.zero; body.rb.angularVelocity = 0; body.rb.bodyType = RigidbodyType2D.Kinematic; }
            int stars = BrokenConnections == 0 && UsedAnchors < Level.anchors ? 3 : BrokenConnections <= 1 ? 2 : 1;
            if (win)
            {
                PlayerPrefs.SetInt("WorkshopStars" + LevelIndex, Mathf.Max(stars, PlayerPrefs.GetInt("WorkshopStars" + LevelIndex)));
                PlayerPrefs.Save();
                Feedback = $"全部气球保住了！评级 {new string('★', stars)}　使用锚点 {UsedAnchors}/{Level.anchors}　断裂 {BrokenConnections}";
            }
            else Feedback = "有气球离开安全区。" + (firstBreak ?? "观察气球的连接与底座固定情况。") + " 点击「保留方案调整」再试。";
        }

        void ConfigureCamera()
        {
            if (viewCamera == null) return;
            viewCamera.orthographic = true;
            viewCamera.rect = new Rect(330f / 1440, 140f / 900, 810f / 1440, 645f / 900);
            viewCamera.orthographicSize = Mathf.Max(4.8f, 6.6f / viewCamera.aspect);
            viewCamera.backgroundColor = new Color(.055f, .10f, .15f);
            viewCamera.cullingMask = 1 << 8;
            float offset = shake > 0 ? Mathf.Sin(Time.unscaledTime * 90) * .04f : 0;
            viewCamera.transform.position = new Vector3(offset, .2f, -10);
            shake = Mathf.Max(0, shake - Time.unscaledDeltaTime);
        }

        void RebuildWorld()
        {
            if (world != null) { world.SetActive(false); DestroyImmediate(world); }
            bodies.Clear(); links.Clear();
            world = new GameObject("AnchorWorkshop_World"); world.transform.SetParent(transform);
            ConfigureCamera();
            BuildPlacementGrid();
            foreach (var block in plan)
            {
                string asset = block.material == MaterialKind.Wood ? "Plank" : block.material == MaterialKind.Steel ? "SteelBeam" : block.material == MaterialKind.Stone ? "StoneWall" : "Balloon";
                var color = block.material == MaterialKind.Balloon ? new Color(1, .68f, .28f) : Color.white;
                var go = Visual(block.material.ToString(), block.cell, Vector2.one * .84f, color, AssetSprite(asset), 5);
                // Collider dimensions are local because Visual normalizes sprite scale.
                var collider = go.AddComponent<BoxCollider2D>();
                collider.size = new Vector2(.84f / go.transform.localScale.x, .84f / go.transform.localScale.y);
                var rb = go.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.mass = block.material == MaterialKind.Stone ? 4 : block.material == MaterialKind.Steel ? 2 : block.material == MaterialKind.Balloon ? .35f : 1;
                rb.gravityScale = block.material == MaterialKind.Balloon ? -.22f : .6f;
                rb.drag = .35f; rb.angularDrag = .8f;
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                var view = new BodyView { plan = block, rb = rb, sprite = go.GetComponent<SpriteRenderer>() };
                bodies.Add(block.cell, view);
                if (block.grounded) MakeLink(view, null, true, true);
            }
            var entries = bodies.Values.ToList();
            for (int i = 0; i < entries.Count; i++) for (int j = i + 1; j < entries.Count; j++)
            {
                if (!Adjacent(entries[i].plan.cell, entries[j].plan.cell)) continue;
                bool reinforced = reinforcements.Any(r => r.Matches(entries[i].plan.cell, entries[j].plan.cell));
                MakeLink(entries[i], entries[j], reinforced, false);
            }
            Physics2D.SyncTransforms();
        }

        void MakeLink(BodyView a, BodyView b, bool reinforced, bool ground)
        {
            var joint = a.rb.gameObject.AddComponent<FixedJoint2D>();
            joint.autoConfigureConnectedAnchor = false;
            // Central anchors keep visual endpoints and solver constraints in agreement.
            joint.anchor = Vector2.zero;
            if (b != null)
            {
                joint.connectedBody = b.rb;
                joint.connectedAnchor = b.rb.transform.InverseTransformPoint(a.rb.position);
            }
            else joint.connectedAnchor = a.rb.position;
            bool steel = a.plan.material == MaterialKind.Steel || (b != null && b.plan.material == MaterialKind.Steel);
            bool stone = a.plan.material == MaterialKind.Stone || (b != null && b.plan.material == MaterialKind.Stone);
            // Forces in the workshop levels are roughly 8-15 N; keep material ratings in that range.
            joint.breakForce = ground ? 240 : reinforced ? 120 : steel ? 78 : stone ? 66 : 52;
            joint.breakTorque = ground ? 1800 : reinforced ? 900 : steel ? 580 : stone ? 480 : 380;
            joint.frequency = 8; joint.dampingRatio = .85f;
            joint.enableCollision = false;
            var line = NewLine(ground ? "地锚" : "连接", ground ? .11f : .065f, 12);
            links.Add(new LinkView { a = a, b = b, joint = joint, line = line, reinforced = reinforced, ground = ground });
        }

        float AccumulatedWindLoad(LinkView link, float direction, float gust)
        {
            Vector2 a = link.a.plan.cell;
            Vector2 b = link.b != null ? link.b.plan.cell : new Vector2(a.x, -3);
            Vector2 midpoint = (a + b) * .5f;
            bool vertical = Mathf.Abs(a.y - b.y) >= Mathf.Abs(a.x - b.x);
            int loadedBlocks = bodies.Values.Count(body => vertical
                ? body.plan.cell.y >= Mathf.Max(a.y, b.y)
                : direction > 0 ? body.plan.cell.x >= midpoint.x : body.plan.cell.x <= midpoint.x);
            return Level.windForce * gust * Mathf.Max(1, loadedBlocks);
        }

        void LateUpdate()
        {
            foreach (var link in links)
            {
                if (link.line == null) continue;
                if (link.broken) { link.line.enabled = false; continue; }
                Vector3 start = link.a.rb.position;
                Vector3 end = link.b != null ? (Vector3)link.b.rb.position : new Vector3(link.a.plan.cell.x, -2.55f);
                link.line.SetPosition(0, start); link.line.SetPosition(1, end);
                Color color = link.ground ? new Color(.7f, .48f, 1) : link.reinforced ? new Color(.25f, .8f, 1) : new Color(.50f, .62f, .65f);
                if (CurrentPhase == Phase.Storm && !link.reinforced) color = Color.Lerp(new Color(.25f, .9f, .6f), Color.red, link.stress);
                link.line.startColor = link.line.endColor = color;
            }
        }

        LineRenderer NewLine(string name, float width, int order)
        {
            var go = new GameObject(name); go.layer = 8; go.transform.SetParent(world.transform);
            var line = go.AddComponent<LineRenderer>(); line.material = lineMaterial;
            line.positionCount = 2; line.startWidth = line.endWidth = width;
            line.sortingOrder = order; line.numCapVertices = 4; return line;
        }

        GameObject Visual(string name, Vector2 position, Vector2 size, Color tint, Sprite sprite, int order)
        {
            var go = new GameObject(name); go.layer = 8; go.transform.SetParent(world.transform);
            go.transform.position = position;
            var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = sprite; sr.color = tint; sr.sortingOrder = order;
            go.transform.localScale = new Vector3(size.x / sprite.bounds.size.x, size.y / sprite.bounds.size.y, 1);
            return go;
        }

        readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();
        Sprite AssetSprite(string name)
        {
            if (spriteCache.TryGetValue(name, out var sprite)) return sprite;
            sprite = Resources.Load<Sprite>("UI/Sprites/Objects/" + name);
            if (sprite == null)
            {
                var texture = Resources.Load<Texture2D>("UI/Sprites/Objects/" + name);
                if (texture != null)
                {
                    sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * .5f, texture.width);
                    ownedAssets.Add(sprite);
                }
            }
            if (sprite == null) sprite = name == "Balloon" ? circle : square;
            spriteCache[name] = sprite; return sprite;
        }

        Sprite MakeSprite(bool round)
        {
            var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            var pixels = new Color[1024];
            for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++)
                pixels[y * 32 + x] = !round || Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 15.5f)) < 15 ? Color.white : Color.clear;
            texture.SetPixels(pixels); texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0, 0, 32, 32), Vector2.one * .5f, 32);
            ownedAssets.Add(sprite); ownedAssets.Add(texture); return sprite;
        }

        void OnDestroy()
        {
            if (viewCamera != null)
            {
                viewCamera.rect = previousCameraRect; viewCamera.transform.position = previousCameraPosition;
                viewCamera.orthographicSize = previousCameraSize; viewCamera.backgroundColor = previousCameraColor;
                viewCamera.cullingMask = previousCameraMask;
            }
            if (world != null) DestroyImmediate(world);
            foreach (var asset in ownedAssets) DestroyOwned(asset);
            foreach (var texture in styleTextures.Values) DestroyOwned(texture);
            DestroyOwned(hudFont);
            if (ownsDefaultLevel) DestroyOwned(Level);
        }

        static void DestroyOwned(Object asset)
        {
            if (asset == null) return;
            if (Application.isPlaying) Destroy(asset);
            else DestroyImmediate(asset);
        }
    }
}
