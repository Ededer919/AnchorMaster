using UnityEngine;
using UnityEngine.UI;

public class PuzzleTile : MonoBehaviour
{
    public int tileValue;
    public int currentRow;
    public int currentCol;

    private Button button;
    private Text buttonText;

    private void Awake()
    {
        button = GetComponent<Button>();
        buttonText = GetComponentInChildren<Text>();

        if (button != null)
        {
            button.onClick.AddListener(OnTileClicked);
        }
    }

    public void Initialize(int value, int row, int col)
    {
        tileValue = value;
        currentRow = row;
        currentCol = col;

        if (buttonText != null)
        {
            buttonText.text = value.ToString();
        }

        gameObject.SetActive(value != 0);
    }

    private void OnTileClicked()
    {
        PuzzleManager puzzleManager = FindObjectOfType<PuzzleManager>();
        if (puzzleManager != null)
        {
            puzzleManager.TryMoveTile(this);
        }
    }

    public void UpdatePosition(int newRow, int newCol)
    {
        currentRow = newRow;
        currentCol = newCol;
    }
}
