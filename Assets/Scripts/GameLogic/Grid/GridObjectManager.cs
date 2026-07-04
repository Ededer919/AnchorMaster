using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages all GridObjects in the scene. Provides registration, queries, and adjacency detection.
/// </summary>
public class GridObjectManager : MonoBehaviour
{
    [Header("Reference")]
    public GridSystem gridSystem;

    private readonly List<GridObject> objects = new List<GridObject>();
    private Transform objectsParent;

    public void Register(GridObject obj)
    {
        if (obj != null && !objects.Contains(obj))
            objects.Add(obj);
    }

    public void SetObjectsParent(Transform parent)
    {
        objectsParent = parent;
    }

    public Transform GetObjectsParent()
    {
        return objectsParent;
    }

    public void Unregister(GridObject obj)
    {
        objects.Remove(obj);
    }

    public List<GridObject> GetAllObjects()
    {
        return new List<GridObject>(objects);
    }

    public GridObject GetObjectAt(int row, int col)
    {
        if (gridSystem == null)
            return null;

        GridCell cell = gridSystem.GetCell(row, col);
        if (cell == null || cell.IsEmpty)
            return null;

        return cell.OccupiedObject;
    }

    public Vector2Int FindNearestEmptyCell(Vector2 worldPos)
    {
        if (gridSystem == null)
            return new Vector2Int(-1, -1);

        GridCell nearest = null;
        float minDist = float.MaxValue;

        for (int r = 0; r < gridSystem.Rows; r++)
        {
            for (int c = 0; c < gridSystem.Cols; c++)
            {
                if (!gridSystem.CanPlaceAt(r, c)) continue;

                var cell = gridSystem.GetCell(r, c);
                if (cell == null) continue;

                float dist = Vector2.Distance(worldPos, cell.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = cell;
                }
            }
        }

        if (nearest != null)
            return new Vector2Int(nearest.Col, nearest.Row);

        return new Vector2Int(-1, -1);
    }

    public List<GridObject> GetAdjacentObjects(GridObject obj)
    {
        List<GridObject> result = new List<GridObject>();

        if (obj == null || !obj.IsPlaced || gridSystem == null)
            return result;

        int row = obj.Row;
        int col = obj.Col;

        GridObject up = GetObjectAt(row + 1, col);
        GridObject down = GetObjectAt(row - 1, col);
        GridObject left = GetObjectAt(row, col - 1);
        GridObject right = GetObjectAt(row, col + 1);

        if (up != null) result.Add(up);
        if (down != null) result.Add(down);
        if (left != null) result.Add(left);
        if (right != null) result.Add(right);

        return result;
    }

    public List<GridObject> GetAdjacentObjects8(GridObject obj)
    {
        List<GridObject> result = new List<GridObject>();

        if (obj == null || !obj.IsPlaced || gridSystem == null)
            return result;

        int row = obj.Row;
        int col = obj.Col;

        for (int dr = -1; dr <= 1; dr++)
        {
            for (int dc = -1; dc <= 1; dc++)
            {
                if (dr == 0 && dc == 0)
                    continue;

                GridObject adjacent = GetObjectAt(row + dr, col + dc);
                if (adjacent != null)
                    result.Add(adjacent);
            }
        }

        return result;
    }

    public List<List<GridObject>> GetConnectedGroups()
    {
        var result = new List<List<GridObject>>();
        var visited = new HashSet<GridObject>();

        foreach (var obj in objects)
        {
            // Skip bedrock — immovable, not part of connected groups
            if (obj == null || !obj.IsPlaced || visited.Contains(obj) || obj.objectType == ObjectType.Bedrock)
                continue;

            var group = new List<GridObject>();
            var queue = new Queue<GridObject>();
            queue.Enqueue(obj);
            visited.Add(obj);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                group.Add(current);

                foreach (var neighbor in GetAdjacentObjects(current))
                {
                    if (!visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }

            result.Add(group);
        }

        return result;
    }

    public void ClearAll()
    {
        foreach (var obj in objects)
        {
            if (obj != null)
                obj.RemoveFromGrid();
        }
    }
}
