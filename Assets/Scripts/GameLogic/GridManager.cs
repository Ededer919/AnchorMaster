using UnityEngine;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [Header("Grid Settings")]
    public Transform gridParent;
    public GameObject anchorSlotPrefab;
    public float cellSize = 1.0f;
    public Vector2 gridOrigin = Vector2.zero;

    private AnchorSlot[,] gridSlots;
    private int rows = 6;
    private int cols = 8;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void InitializeGrid(int rows, int cols)
    {
        this.rows = rows;
        this.cols = cols;
        gridSlots = new AnchorSlot[rows, cols];

        ClearGrid();

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                Vector2 worldPos = GridToWorldPosition(r, c);
                GameObject slotObj = Instantiate(anchorSlotPrefab, gridParent);
                slotObj.transform.position = worldPos;

                AnchorSlot slot = slotObj.GetComponent<AnchorSlot>();
                slot.Initialize(r, c);
                gridSlots[r, c] = slot;
            }
        }
    }

    public Vector2 GridToWorldPosition(int row, int col)
    {
        float x = gridOrigin.x + col * cellSize;
        float y = gridOrigin.y + row * cellSize;
        return new Vector2(x, y);
    }

    public Vector2Int WorldToGridPosition(Vector2 worldPos)
    {
        int col = Mathf.RoundToInt((worldPos.x - gridOrigin.x) / cellSize);
        int row = Mathf.RoundToInt((worldPos.y - gridOrigin.y) / cellSize);
        col = Mathf.Clamp(col, 0, cols - 1);
        row = Mathf.Clamp(row, 0, rows - 1);
        return new Vector2Int(col, row);
    }

    public AnchorSlot GetSlot(int row, int col)
    {
        if (row >= 0 && row < rows && col >= 0 && col < cols)
        {
            return gridSlots[row, col];
        }
        return null;
    }

    public List<AnchorSlot> GetAllSlots()
    {
        List<AnchorSlot> slots = new List<AnchorSlot>();
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                slots.Add(gridSlots[r, c]);
            }
        }
        return slots;
    }

    public AnchorSlot GetNearestSlot(Vector2 worldPos)
    {
        Vector2Int gridPos = WorldToGridPosition(worldPos);
        return GetSlot(gridPos.y, gridPos.x);
    }

    public void HighlightValidSlots(bool highlight)
    {
        foreach (var slot in GetAllSlots())
        {
            if (slot != null && !slot.IsOccupied)
            {
                slot.SetHighlighted(highlight);
            }
        }
    }

    private void ClearGrid()
    {
        if (gridSlots != null)
        {
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    if (gridSlots[r, c] != null)
                    {
                        Destroy(gridSlots[r, c].gameObject);
                    }
                }
            }
        }
    }
}
