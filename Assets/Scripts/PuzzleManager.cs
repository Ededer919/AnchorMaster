using UnityEngine;
using System.Collections.Generic;

public class PuzzleManager : MonoBehaviour
{
    [Header("Puzzle Settings")]
    public Transform puzzleParent;
    public GameObject tilePrefab;

    private int[,] puzzleBoard;
    private List<PuzzleTile> tiles;
    private int emptyRow;
    private int emptyCol;
    private int moveCount;
    private float gameTimer;
    private bool isGameActive;

    private void Start()
    {
        InitializePuzzle();
    }

    private void Update()
    {
        if (isGameActive)
        {
            gameTimer += Time.deltaTime;
            UIManager.Instance?.UpdateTimer(gameTimer);
        }
    }

    public void InitializePuzzle()
    {
        moveCount = 0;
        gameTimer = 0f;
        isGameActive = true;

        int gridSize = GameManager.Instance.gridSize;
        puzzleBoard = new int[gridSize, gridSize];
        tiles = new List<PuzzleTile>();

        CreatePuzzleTiles(gridSize);
        ShufflePuzzle(gridSize);

        UIManager.Instance?.UpdateMoveCount(moveCount);
        UIManager.Instance?.UpdateStatus("Puzzle Ready!");
    }

    private void CreatePuzzleTiles(int gridSize)
    {
        for (int row = 0; row < gridSize; row++)
        {
            for (int col = 0; col < gridSize; col++)
            {
                int value = row * gridSize + col + 1;
                if (value == gridSize * gridSize)
                {
                    value = 0;
                }

                puzzleBoard[row, col] = value;

                if (value != 0)
                {
                    GameObject tileObj = Instantiate(tilePrefab, puzzleParent);
                    PuzzleTile tile = tileObj.GetComponent<PuzzleTile>();
                    tile.Initialize(value, row, col);
                    tiles.Add(tile);
                }
                else
                {
                    emptyRow = row;
                    emptyCol = col;
                }
            }
        }
    }

    private void ShufflePuzzle(int gridSize)
    {
        int shuffleMoves = gridSize * gridSize * 10;
        for (int i = 0; i < shuffleMoves; i++)
        {
            int randomDir = Random.Range(0, 4);
            MoveEmpty(randomDir);
        }
    }

    private void MoveEmpty(int direction)
    {
        int newRow = emptyRow;
        int newCol = emptyCol;

        switch (direction)
        {
            case 0: newRow--; break;
            case 1: newRow++; break;
            case 2: newCol--; break;
            case 3: newCol++; break;
        }

        int gridSize = GameManager.Instance.gridSize;
        if (newRow >= 0 && newRow < gridSize && newCol >= 0 && newCol < gridSize)
        {
            puzzleBoard[emptyRow, emptyCol] = puzzleBoard[newRow, newCol];
            puzzleBoard[newRow, newCol] = 0;

            PuzzleTile tile = FindTileAtPosition(newRow, newCol);
            if (tile != null)
            {
                tile.UpdatePosition(emptyRow, emptyCol);
            }

            emptyRow = newRow;
            emptyCol = newCol;
        }
    }

    public void TryMoveTile(PuzzleTile tile)
    {
        if (!isGameActive) return;

        int rowDiff = Mathf.Abs(tile.currentRow - emptyRow);
        int colDiff = Mathf.Abs(tile.currentCol - emptyCol);

        if ((rowDiff == 1 && colDiff == 0) || (rowDiff == 0 && colDiff == 1))
        {
            MoveTileToEmpty(tile);
            moveCount++;
            UIManager.Instance?.UpdateMoveCount(moveCount);

            if (CheckWinCondition())
            {
                isGameActive = false;
                GameManager.Instance?.OnPuzzleSolved();
            }
        }
    }

    private void MoveTileToEmpty(PuzzleTile tile)
    {
        int tileRow = tile.currentRow;
        int tileCol = tile.currentCol;

        puzzleBoard[emptyRow, emptyCol] = tile.tileValue;
        puzzleBoard[tileRow, tileCol] = 0;

        tile.UpdatePosition(emptyRow, emptyCol);

        emptyRow = tileRow;
        emptyCol = tileCol;
    }

    private PuzzleTile FindTileAtPosition(int row, int col)
    {
        foreach (var tile in tiles)
        {
            if (tile.currentRow == row && tile.currentCol == col)
            {
                return tile;
            }
        }
        return null;
    }

    private bool CheckWinCondition()
    {
        int gridSize = GameManager.Instance.gridSize;
        int expected = 1;

        for (int row = 0; row < gridSize; row++)
        {
            for (int col = 0; col < gridSize; col++)
            {
                if (row == gridSize - 1 && col == gridSize - 1)
                {
                    return puzzleBoard[row, col] == 0;
                }

                if (puzzleBoard[row, col] != expected)
                {
                    return false;
                }
                expected++;
            }
        }
        return true;
    }
}
