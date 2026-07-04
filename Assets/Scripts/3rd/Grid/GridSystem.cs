using UnityEngine;
using System.Collections.Generic;

public class GridSystem : MonoBehaviour
{
    [Header("Grid Settings")]
    public float cellSize = 1.0f;
    public Vector2 gridOrigin = Vector2.zero;

    private GridCell[,] cells;
    private int rows;
    private int cols;

    public int Rows => rows;
    public int Cols => cols;

    public void InitializeGrid(int rows, int cols, GameObject cellPrefab, Transform parent)
    {
        this.rows = rows;
        this.cols = cols;
        cells = new GridCell[rows, cols];

        ClearGrid();

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                Vector2 worldPos = GridToWorldPosition(r, c);
                GameObject cellObj = Instantiate(cellPrefab, parent);
                cellObj.transform.position = worldPos;
                cellObj.SetActive(true);

                GridCell cell = cellObj.GetComponent<GridCell>();
                cell.Initialize(r, c);
                cells[r, c] = cell;
            }
        }
    }

    public void SetCell(int row, int col, GridCell cell)
    {
        if (row >= 0 && row < rows && col >= 0 && col < cols)
        {
            cells[row, col] = cell;
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

    public GridCell GetCell(int row, int col)
    {
        if (row >= 0 && row < rows && col >= 0 && col < cols)
        {
            return cells[row, col];
        }
        return null;
    }

    public List<GridCell> GetAllCells()
    {
        List<GridCell> result = new List<GridCell>();
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (cells[r, c] != null)
                {
                    result.Add(cells[r, c]);
                }
            }
        }
        return result;
    }

    public GridCell GetNearestCell(Vector2 worldPos)
    {
        Vector2Int gridPos = WorldToGridPosition(worldPos);
        return GetCell(gridPos.y, gridPos.x);
    }

    public void HighlightValidCells(bool highlight)
    {
        foreach (var cell in GetAllCells())
        {
            if (cell != null)
            {
                cell.SetHighlighted(highlight);
            }
        }
    }

    public bool IsCellOccupied(int row, int col)
    {
        GridCell cell = GetCell(row, col);
        return cell != null && !cell.IsEmpty;
    }

    public bool CanPlaceAt(int row, int col)
    {
        return row >= 0 && row < rows && col >= 0 && col < cols && GetCell(row, col) != null && GetCell(row, col).IsEmpty;
    }

    public List<GridCell> GetEmptyCells()
    {
        List<GridCell> result = new List<GridCell>();
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (cells[r, c] != null && cells[r, c].IsEmpty)
                {
                    result.Add(cells[r, c]);
                }
            }
        }
        return result;
    }

    public void SwapCells(int row1, int col1, int row2, int col2)
    {
        GridCell temp = cells[row1, col1];
        cells[row1, col1] = cells[row2, col2];
        cells[row2, col2] = temp;

        if (cells[row1, col1] != null)
        {
            cells[row1, col1].Initialize(row1, col1);
        }
        if (cells[row2, col2] != null)
        {
            cells[row2, col2].Initialize(row2, col2);
        }
    }

    private void ClearGrid()
    {
        if (cells != null)
        {
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    if (cells[r, c] != null)
                    {
                        Destroy(cells[r, c].gameObject);
                    }
                }
            }
        }
    }
}
