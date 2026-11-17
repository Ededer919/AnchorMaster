using System.Linq;
using AnchorMaster.Workshop;
using UnityEditor;
using UnityEngine;

public static class WorkshopSmokeTest
{
    public static void Run()
    {
        bool hadStars = PlayerPrefs.HasKey("WorkshopStars0");
        int oldStars = PlayerPrefs.GetInt("WorkshopStars0");
        var previousSimulationMode = Physics2D.simulationMode;
        var cameraObject = new GameObject("Workshop Smoke Camera", typeof(Camera));
        cameraObject.tag = "MainCamera";
        var root = new GameObject("Workshop Smoke Test");
        Physics2D.simulationMode = SimulationMode2D.Script;
        try
        {
            var game = root.AddComponent<AnchorPuzzleGame>();
            game.Initialize(0);
            Require(game.Level.title == "第一枚地锚", "Level one loads");
            Require(game.LevelCount >= 5, "Workshop levels are discovered from Resources");
            Require(game.UsedAnchors == 0, "Initial anchor count");
            Require(root.GetComponentsInChildren<Collider2D>().Length == game.Blueprint.Count,
                "Only blueprint blocks have colliders; no bedrock or invisible floor");
            Require(root.GetComponentsInChildren<SpriteRenderer>().Count(s => s.name == "Grid Cell") == 66,
                "All 11 by 6 placement cells are visible");
            Require(game.Place(MaterialKind.Steel, new Vector2Int(2, 2)), "Place steel");
            Require(game.Remaining(MaterialKind.Steel) == 1, "Quota consumed");
            game.Undo();
            Require(game.Remaining(MaterialKind.Steel) == 2, "Undo refunds material");
            Require(game.SetGroundAnchor(new Vector2Int(0, -2)), "Place ground anchor");
            Require(game.UsedAnchors == 1, "Anchor quota consumed");

            game.StartTrial();
            for (int i = 0; i < 12; i++)
            {
                Physics2D.Simulate(1f / 60f);
                game.StepTrial(1f / 60f);
            }
            Require(game.CurrentPhase == AnchorPuzzleGame.Phase.Storm, "Storm simulation advances");
            Require(game.BrokenConnections == 0, "Grounded tower holds for the first storm beat");
            AdvanceTrial(game, 650);
            Require(game.CurrentPhase == AnchorPuzzleGame.Phase.Result && game.Won,
                $"Ground anchor holds the full storm (phase={game.CurrentPhase}, breaks={game.BrokenConnections}, feedback={game.Feedback})");

            game.Initialize(0);
            Require(game.Level.title == "第一枚地锚", "Level one reloads");
            game.StartTrial();
            AdvanceTrial(game, 650);
            Require(game.CurrentPhase == AnchorPuzzleGame.Phase.Result && !game.Won, "Tower without a ground anchor loses the full storm");

            game.Initialize(0);
            game.Level.windForce = 70;
            game.StartTrial();
            AdvanceTrial(game, 360);
            Require(game.BrokenConnections > 0,
                $"Strong wind breaks an unanchored wood connection (breaks={game.BrokenConnections}, phase={game.CurrentPhase})");

            game.Initialize(3);
            Require(game.Level.reversingWind, "Reversing wind level loads");
            var target = game.Blueprint.First(b => b.material == MaterialKind.Balloon);
            target.cell = new Vector2Int(9, 0);
            game.ReturnToBuild();
            game.StartTrial();
            Physics2D.Simulate(1f / 60f);
            game.StepTrial(1f / 60f);
            Require(game.CurrentPhase == AnchorPuzzleGame.Phase.Result && !game.Won, "Lost target fails the trial");
            game.Initialize(game.LevelCount - 1);
            Require(game.Level.title == "最后的两枚锚", "The last discovered workshop level loads");
            Debug.Log("[Workshop Smoke Test] PASS: levels, placement, undo, anchors, joints, victory and loss.");
        }
        finally
        {
            if (hadStars) PlayerPrefs.SetInt("WorkshopStars0", oldStars);
            else PlayerPrefs.DeleteKey("WorkshopStars0");
            PlayerPrefs.Save();
            Physics2D.simulationMode = previousSimulationMode;
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(cameraObject);
        }
    }

    static void Require(bool condition, string assertion)
    {
        if (!condition) throw new System.InvalidOperationException("Workshop smoke test failed: " + assertion);
    }

    static void AdvanceTrial(AnchorPuzzleGame game, int frames)
    {
        for (int i = 0; i < frames && game.CurrentPhase == AnchorPuzzleGame.Phase.Storm; i++)
        {
            Physics2D.Simulate(1f / 60f);
            game.StepTrial(1f / 60f);
        }
    }
}
