using UnityEngine;

public class GridCell : MonoBehaviour
{
    public int Row { get; protected set; }
    public int Col { get; protected set; }
    public bool IsHighlighted { get; private set; }
    public bool IsSelected { get; set; }

    [Header("Visual Settings")]
    public SpriteRenderer backgroundRenderer;
    public SpriteRenderer highlightRenderer;

    protected Color normalColor = new Color(0.2f, 0.2f, 0.3f, 0.3f);
    protected Color highlightColor = new Color(0.376f, 0.329f, 0.945f, 0.5f);

    public virtual void Initialize(int row, int col)
    {
        Row = row;
        Col = col;
        IsHighlighted = false;
        IsSelected = false;

        if (backgroundRenderer != null)
        {
            backgroundRenderer.color = normalColor;
        }

        if (highlightRenderer != null)
        {
            highlightRenderer.gameObject.SetActive(false);
        }
    }

    public virtual void SetHighlighted(bool highlighted)
    {
        IsHighlighted = highlighted;

        if (highlightRenderer != null)
        {
            highlightRenderer.gameObject.SetActive(highlighted);
            highlightRenderer.color = highlightColor;
        }
    }

    public virtual void OnCellClicked()
    {
    }
}
