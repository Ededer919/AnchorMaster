using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles merging connected GridObjects into physics bodies and dissolving them back.
/// Each object type contributes its own mass, gravity, friction, and bounciness.
/// Passive abilities (LightningRod, WindShield) are applied after merge.
/// </summary>
public class PhysicsMergeSystem : MonoBehaviour
{
    private GridObjectManager objectManager;
    private GridSystem gridSystem;
    private WindSystem2D windSystem;
    private BuildSystem buildSystem;
    private readonly List<PhysicsGroup> physicsGroups = new List<PhysicsGroup>();
    private bool physicsMode;

    public bool IsPhysicsMode => physicsMode;

    public void Initialize(GridObjectManager manager, GridSystem grid)
    {
        objectManager = manager;
        gridSystem = grid;
    }

    public void SetWindSystem(WindSystem2D wind)
    {
        windSystem = wind;
    }

    public void SetBuildSystem(BuildSystem build)
    {
        buildSystem = build;
    }

    /// <summary>
    /// Merges connected GridObjects into physics groups.
    /// </summary>
    public void MergeIntoPhysics()
    {
        if (physicsMode) return;
        physicsMode = true;

        // Clean up leftover empty physics groups
        var leftover = GetComponentsInChildren<Rigidbody2D>();
        foreach (var oldRb in leftover)
        {
            if (oldRb.transform.childCount == 0)
                Destroy(oldRb.gameObject);
        }

        var groups = objectManager.GetConnectedGroups();
        Debug.Log($"[Physics] Found {groups.Count} connected groups, merging...");

        Transform parent = objectManager.GetObjectsParent();

        foreach (var group in groups)
        {
            if (group.Count == 0) continue;

            // Calculate center of mass (weighted by mass)
            Vector2 center = Vector2.zero;
            float totalMass = 0f;
            float totalGravity = 0f;

            foreach (var obj in group)
            {
                var config = ObjectConfig.Get(obj.objectType);
                center += (Vector2)obj.transform.position * config.mass;
                totalMass += config.mass;
                totalGravity += config.gravityScale;
            }
            center /= totalMass;
            float avgGravity = totalGravity / group.Count;

            // Create physics parent
            var physGo = new GameObject("PhysicsGroup_" + physicsGroups.Count);
            if (parent != null)
                physGo.transform.SetParent(parent, false);
            physGo.transform.position = center;

            var rb = physGo.AddComponent<Rigidbody2D>();
            rb.gravityScale = avgGravity;
            rb.mass = totalMass;
            rb.drag = 0.5f;
            rb.angularDrag = 0.5f;

            // Add wind receiver to the physics group so wind applies force
            var windReceiver = physGo.AddComponent<WindReceiver2D>();
            // Average wind sensitivity of group members
            float avgWindSens = 0f;
            foreach (var obj in group)
            {
                var config = ObjectConfig.Get(obj.objectType);
                avgWindSens += config.windSensitivity;
            }
            avgWindSens /= group.Count;
            windReceiver.windSensitivity = avgWindSens;

            // Parent objects and add colliders
            foreach (var obj in group)
            {
                obj.RemoveFromGrid();
                obj.EnablePhysicsMode();
                obj.transform.SetParent(physGo.transform, true);
            }

            foreach (var obj in group)
            {
                var config = ObjectConfig.Get(obj.objectType);
                var col = physGo.AddComponent<BoxCollider2D>();
                col.offset = obj.transform.localPosition;
                col.size = Vector2.one * gridSystem.cellSize;

                // Apply material based on type
                if (config.bounciness > 0f || config.friction > 1f)
                {
                    var mat = new PhysicsMaterial2D(config.name);
                    mat.bounciness = config.bounciness;
                    mat.friction = config.friction;
                    col.sharedMaterial = mat;
                }
            }

            physicsGroups.Add(new PhysicsGroup { root = physGo.transform, rigidbody = rb, objects = group, isOrphan = false });
            Debug.Log($"[Physics] Group {physicsGroups.Count}: {group.Count} objects, mass={totalMass:F1}, gravity={avgGravity:F2}");
        }

        // Handle orphan objects (not connected to any group) — give them individual physics
        CreateOrphanPhysics();
    }

    /// <summary>
    /// Creates individual physics bodies for objects not in any connected group.
    /// </summary>
    private void CreateOrphanPhysics()
    {
        var allObjects = objectManager.GetAllObjects();
        var groupedObjects = new HashSet<GridObject>();
        foreach (var g in physicsGroups)
            foreach (var o in g.objects)
                if (o != null) groupedObjects.Add(o);

        Transform parent = objectManager.GetObjectsParent();

        foreach (var obj in allObjects)
        {
            if (obj == null || groupedObjects.Contains(obj)) continue;

            // Skip staged objects (not yet placed on grid — immune to wind)
            if (buildSystem != null && buildSystem.IsStaged(obj)) continue;

            // Skip Bedrock — immovable terrain, not affected by wind or physics
            if (obj.objectType == ObjectType.Bedrock) continue;

            var config = ObjectConfig.Get(obj.objectType);

            if (obj.IsPlaced)
                obj.RemoveFromGrid();
            obj.EnablePhysicsMode();
            if (parent != null)
                obj.transform.SetParent(parent, true);

            var rb = obj.gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = config.gravityScale;
            rb.mass = config.mass;
            rb.drag = 0.5f;
            rb.angularDrag = 0.5f;

            var col = obj.gameObject.AddComponent<BoxCollider2D>();
            col.size = Vector2.one * gridSystem.cellSize * 0.9f;
            if (config.bounciness > 0f || config.friction > 1f)
            {
                var mat = new PhysicsMaterial2D(config.name);
                mat.bounciness = config.bounciness;
                mat.friction = config.friction;
                col.sharedMaterial = mat;
            }

            var receiver = obj.gameObject.AddComponent<WindReceiver2D>();
            receiver.windSensitivity = config.windSensitivity;

            // Track as a single-object group for dissolution
            var orphanGroup = new PhysicsGroup
            {
                root = obj.transform,
                rigidbody = rb,
                objects = new List<GridObject> { obj },
                isOrphan = true
            };
            physicsGroups.Add(orphanGroup);

            Debug.Log($"[Physics] Orphan: {obj.displayName} ({obj.objectType}), mass={config.mass}");
        }
    }

    /// <summary>
    /// Dissolves all physics groups, returning objects to free state.
    /// </summary>
    public void DissolvePhysics()
    {
        if (!physicsMode) return;
        physicsMode = false;

        Transform parent = objectManager.GetObjectsParent();

        foreach (var group in physicsGroups)
        {
            if (group.root == null) continue;

            if (group.isOrphan)
            {
                // Physics components are on the object itself — just remove them
                foreach (var obj in group.objects)
                {
                    if (obj == null) continue;

                    if (parent != null)
                        obj.transform.SetParent(parent, true);
                    obj.DisablePhysicsMode();

                    var config = ObjectConfig.Get(obj.objectType);
                    obj.windSensitivity = config.windSensitivity;

                    // Remove runtime physics components (order matters: WindReceiver before Rigidbody)
                    var wr = obj.GetComponent<WindReceiver2D>();
                    if (wr != null) DestroyImmediate(wr);
                    var rb = obj.GetComponent<Rigidbody2D>();
                    if (rb != null) DestroyImmediate(rb);
                    // Remove extra colliders added during merge (keep original one)
                    var colliders = obj.GetComponents<BoxCollider2D>();
                    for (int i = colliders.Length - 1; i >= 1; i--)
                        DestroyImmediate(colliders[i]);
                }
            }
            else
            {
                // Normal group — detach objects, destroy parent container
                foreach (var obj in group.objects)
                {
                    if (obj == null) continue;

                    if (parent != null)
                        obj.transform.SetParent(parent, true);
                    obj.DisablePhysicsMode();

                    var config = ObjectConfig.Get(obj.objectType);
                    obj.windSensitivity = config.windSensitivity;
                }

                if (group.root != null)
                    Destroy(group.root.gameObject);
            }
        }

        physicsGroups.Clear();
        Debug.Log("[Physics] Physics groups dissolved.");
    }

    /// <summary>
    /// Checks if any Balloon objects survived (still within grid bounds).
    /// Balloon blown out of grid = fail.
    /// </summary>
    public bool CheckBalloonSurvival(float groundY)
    {
        float gridMinX = gridSystem.gridOrigin.x - gridSystem.cellSize;
        float gridMaxX = gridSystem.gridOrigin.x + gridSystem.Cols * gridSystem.cellSize + gridSystem.cellSize;
        float gridMinY = gridSystem.gridOrigin.y - gridSystem.cellSize;
        float gridMaxY = gridSystem.gridOrigin.y + gridSystem.Rows * gridSystem.cellSize + gridSystem.cellSize;

        foreach (var group in physicsGroups)
        {
            foreach (var obj in group.objects)
            {
                if (obj == null || obj.objectType != ObjectType.Balloon) continue;

                Vector3 pos = obj.transform.position;
                bool inBounds = pos.x > gridMinX && pos.x < gridMaxX && pos.y > gridMinY && pos.y < gridMaxY;

                if (inBounds)
                {
                    Debug.Log($"[Physics] Balloon survived at ({pos.x:F1}, {pos.y:F1})");
                    return true;
                }
                else
                {
                    Debug.Log($"[Physics] Balloon lost! pos=({pos.x:F1}, {pos.y:F1})");
                }
            }
        }

        // Also check staged/orphan balloons not in physics groups
        var allObjs = objectManager.GetAllObjects();
        foreach (var obj in allObjs)
        {
            if (obj == null || obj.objectType != ObjectType.Balloon) continue;
            if (buildSystem != null && buildSystem.IsStaged(obj)) continue;

            Vector3 pos = obj.transform.position;
            if (pos.x > gridMinX && pos.x < gridMaxX && pos.y > gridMinY && pos.y < gridMaxY)
            {
                Debug.Log($"[Physics] Balloon (unmerged) survived at ({pos.x:F1}, {pos.y:F1})");
                return true;
            }
        }

        return false;
    }

    private class PhysicsGroup
    {
        public Transform root;
        public Rigidbody2D rigidbody;
        public List<GridObject> objects;
        public bool isOrphan; // true if physics components are on the object itself
    }
}
