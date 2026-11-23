using System.Collections.Generic;
using UnityEngine;

namespace AnchorMaster.Workshop
{
    public partial class AnchorPuzzleGame
    {
        GameObject placementPreview;
        SpriteRenderer previewTile, previewBlock;
        LineRenderer previewOutline;
        readonly List<SceneryCloud> sceneryClouds = new List<SceneryCloud>();

        class SceneryCloud
        {
            public Transform transform;
            public float speed, originX, range;
        }

        void BuildScenery()
        {
            sceneryClouds.Clear();
            float height = viewCamera.orthographicSize * 2;
            float width = height * viewCamera.aspect;
            Visual("Artwork Sky", viewCamera.transform.position, new Vector2(width * 1.08f, height * 1.08f),
                Color.white, AssetSprite("sky1"), -20);
            Visual("Artwork Sun", new Vector2(-width * .36f, 4.45f), Vector2.one * .9f,
                Color.white, AssetSprite("sun"), -11);
            AddSceneryCloud(new Vector2(-2.7f, 4.25f), new Vector2(3.4f, 1.2f), .12f, width * .18f);
            AddSceneryCloud(new Vector2(4.1f, 3.85f), new Vector2(2.4f, .86f), -.08f, width * .12f);
        }

        void AddSceneryCloud(Vector2 position, Vector2 size, float speed, float range)
        {
            var cloud = Visual("Artwork Cloud", position, size, Color.white, AssetSprite("cloud"), -10);
            sceneryClouds.Add(new SceneryCloud { transform = cloud.transform, speed = speed, originX = position.x, range = range });
        }

        void AnimateScenery(float deltaTime)
        {
            foreach (var cloud in sceneryClouds)
            {
                if (cloud.transform == null) continue;
                var position = cloud.transform.position;
                position.x += cloud.speed * deltaTime;
                if (position.x > cloud.originX + cloud.range) position.x = cloud.originX - cloud.range;
                else if (position.x < cloud.originX - cloud.range) position.x = cloud.originX + cloud.range;
                cloud.transform.position = position;
            }
        }

        void BuildPlacementGrid()
        {
            Visual("Grid Shadow", new Vector2(0, .43f), new Vector2(11.22f, 6.22f),
                new Color(.025f, .055f, .085f, .52f), square, -9);
            for (int x = -5; x <= 5; x++)
                for (int y = -2; y <= 3; y++)
                    Visual("Grid Cell", new Vector2(x, y), Vector2.one * .98f,
                        (x + y) % 2 == 0 ? new Color(.085f, .15f, .20f, .78f) : new Color(.095f, .165f, .215f, .78f), square, -8);

            var ink = new Color(.25f, .48f, .56f, .58f);
            for (int x = -5; x <= 6; x++)
                GridLine("Grid Vertical", new Vector2(x - .5f, -2.5f), new Vector2(x - .5f, 3.5f), .012f, ink);
            for (int y = -2; y <= 4; y++)
                GridLine("Grid Horizontal", new Vector2(-5.5f, y - .5f), new Vector2(5.5f, y - .5f), .012f, ink);

            var border = NewLine("Grid Border", .035f, -6);
            border.positionCount = 4; border.loop = true;
            border.SetPositions(new[] { new Vector3(-5.5f, -2.5f), new Vector3(-5.5f, 3.5f),
                new Vector3(5.5f, 3.5f), new Vector3(5.5f, -2.5f) });
            border.startColor = border.endColor = new Color(.32f, .64f, .69f, .8f);

            // A visual baseline only: there are no bedrock blocks or hidden floor colliders.
            GridLine("Ground Baseline", new Vector2(-5.65f, -2.55f), new Vector2(5.65f, -2.55f),
                .06f, new Color(.40f, .79f, .73f));
            for (int x = -5; x <= 5; x++)
                GridLine("Ground Tick", new Vector2(x, -2.58f), new Vector2(x, -2.72f),
                    .025f, new Color(.28f, .52f, .53f));

            placementPreview = new GameObject("Placement Preview");
            placementPreview.transform.SetParent(world.transform);
            var tile = Visual("Hover Cell", Vector2.zero, Vector2.one * .94f, Color.white, square, 2);
            tile.transform.SetParent(placementPreview.transform, false);
            previewTile = tile.GetComponent<SpriteRenderer>();
            var ghost = Visual("Material Preview", Vector2.zero, Vector2.one * .78f, Color.white, square, 6);
            ghost.transform.SetParent(placementPreview.transform, false);
            previewBlock = ghost.GetComponent<SpriteRenderer>();
            previewOutline = NewLine("Hover Outline", .045f, 15);
            previewOutline.transform.SetParent(placementPreview.transform, false);
            previewOutline.useWorldSpace = false; previewOutline.loop = true; previewOutline.positionCount = 4;
            previewOutline.SetPositions(new[] { new Vector3(-.46f, -.46f), new Vector3(-.46f, .46f),
                new Vector3(.46f, .46f), new Vector3(.46f, -.46f) });
            placementPreview.SetActive(false);
        }

        void GridLine(string name, Vector2 from, Vector2 to, float width, Color color)
        {
            var line = NewLine(name, width, -6);
            line.SetPosition(0, from); line.SetPosition(1, to);
            line.startColor = line.endColor = color;
        }

        void UpdatePlacementPreview(Vector2Int cell)
        {
            bool materialTool = tool == WorkshopTool.Wood || tool == WorkshopTool.Steel || tool == WorkshopTool.Stone;
            bool moving = draggedCell.HasValue;
            // Keep feedback near the board, and hide it while using side panels.
            if (Mathf.Abs(cell.x) > 6 || cell.y < -3 || cell.y > 4) return;
            var kind = tool == WorkshopTool.Steel ? MaterialKind.Steel : tool == WorkshopTool.Stone ? MaterialKind.Stone : MaterialKind.Wood;
            bool valid = CanPlace(cell) && (!materialTool || Remaining(kind) > 0);
            if (moving) valid = CanPlace(cell) || cell == draggedCell.Value;
            if (!materialTool && !moving)
            {
                var block = plan.Find(b => b.cell == cell);
                valid = block != null;
                if (tool == WorkshopTool.GroundAnchor)
                    valid = block != null && cell.y == -2 && block.material != MaterialKind.Balloon &&
                        (block.grounded || UsedAnchors < Level.anchors);
                else if (tool == WorkshopTool.Move) valid = block != null && !block.preset;
                else if (tool == WorkshopTool.Reinforce && selectedEndpoint.HasValue)
                    valid = block != null && Adjacent(selectedEndpoint.Value, cell);
            }
            var tint = valid ? new Color(.30f, .94f, .74f) : new Color(1, .36f, .32f);
            placementPreview.transform.position = (Vector2)cell;
            placementPreview.SetActive(true);
            previewTile.color = new Color(tint.r, tint.g, tint.b, .16f);
            previewOutline.startColor = previewOutline.endColor = tint;
            previewBlock.enabled = materialTool && !moving && !bodies.ContainsKey(cell);
            if (previewBlock.enabled)
            {
                previewBlock.sprite = AssetSprite(kind == MaterialKind.Wood ? "Plank" : kind == MaterialKind.Steel ? "SteelBeam" : "StoneWall");
                var size = previewBlock.sprite.bounds.size;
                previewBlock.transform.localScale = new Vector3(.78f / size.x, .78f / size.y, 1);
                previewBlock.color = new Color(tint.r, tint.g, tint.b, .5f);
            }
        }
    }
}
