using UnityEngine;
using UnityEngine.UI;

public class AnchorSlot : MonoBehaviour
{
    public int Row { get; private set; }
    public int Col { get; private set; }
    public bool IsOccupied { get; private set; }
    public bool IsValid { get; set; } = true;

    [Header("Visual Settings")]
    public SpriteRenderer backgroundRenderer;
    public SpriteRenderer highlightRenderer;
    public GameObject anchorPrefab;

    private GameObject currentAnchor;
    private Color normalColor = new Color(0.2f, 0.2f, 0.3f, 0.3f);
    private Color highlightColor = new Color(0.376f, 0.329f, 0.945f, 0.5f);
    private Color occupiedColor = new Color(0.376f, 0.329f, 0.945f, 1f);

    public void Initialize(int row, int col)
    {
        Row = row;
        Col = col;
        IsOccupied = false;

        if (backgroundRenderer != null)
        {
            backgroundRenderer.color = normalColor;
        }

        if (highlightRenderer != null)
        {
            highlightRenderer.gameObject.SetActive(false);
        }
    }

    public void SetHighlighted(bool highlighted)
    {
        if (!IsValid || IsOccupied) return;

        if (highlightRenderer != null)
        {
            highlightRenderer.gameObject.SetActive(highlighted);
            highlightRenderer.color = highlightColor;
        }
    }

    public bool PlaceAnchor()
    {
        if (IsOccupied || !IsValid) return false;

        IsOccupied = true;

        if (anchorPrefab != null)
        {
            currentAnchor = Instantiate(anchorPrefab, transform);
            currentAnchor.transform.localPosition = Vector3.zero;
        }

        if (backgroundRenderer != null)
        {
            backgroundRenderer.color = occupiedColor;
        }

        if (highlightRenderer != null)
        {
            highlightRenderer.gameObject.SetActive(false);
        }

        return true;
    }

    public bool RemoveAnchor()
    {
        if (!IsOccupied) return false;

        IsOccupied = false;

        if (currentAnchor != null)
        {
            Destroy(currentAnchor);
            currentAnchor = null;
        }

        if (backgroundRenderer != null)
        {
            backgroundRenderer.color = normalColor;
        }

        return true;
    }

    public GameObject GetCurrentAnchor()
    {
        return currentAnchor;
    }
}
