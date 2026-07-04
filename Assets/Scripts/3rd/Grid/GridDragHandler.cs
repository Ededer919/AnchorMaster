using UnityEngine;
using System;

public class GridDragHandler : MonoBehaviour
{
    [Header("Drag Settings")]
    public float snapDistance = 0.5f;
    public float dragSpeed = 10f;

    [Header("Visual Feedback")]
    public GameObject dragIndicator;
    public LineRenderer connectionLine;
    public Color validColor = Color.green;
    public Color invalidColor = Color.red;

    public event Action<GridCell> OnCellSelected;
    public event Action<GridCell> OnCellDropped;
    public event Action OnDragStarted;
    public event Action OnDragStopped;

    private bool isDragging = false;
    private GridCell currentCell;
    private GridCell nearestCell;
    private Vector2 dragOffset;

    public bool IsDragging => isDragging;
    public GridCell CurrentCell => currentCell;
    public GridCell NearestCell => nearestCell;

    public void StartDrag(GridCell cell, Vector2 worldPos)
    {
        isDragging = true;
        currentCell = cell;
        nearestCell = cell;

        if (dragIndicator != null)
        {
            dragIndicator.SetActive(true);
            dragIndicator.transform.position = worldPos;
        }

        if (connectionLine != null)
        {
            connectionLine.enabled = true;
            connectionLine.SetPosition(0, cell.transform.position);
            connectionLine.SetPosition(1, worldPos);
        }

        OnDragStarted?.Invoke();
    }

    public void UpdateDrag(Vector2 worldPos)
    {
        if (!isDragging) return;

        if (dragIndicator != null)
        {
            dragIndicator.transform.position = worldPos;
        }

        if (connectionLine != null && currentCell != null)
        {
            connectionLine.SetPosition(1, worldPos);

            float distance = Vector2.Distance(currentCell.transform.position, worldPos);
            Color lineColor = distance <= snapDistance ? validColor : invalidColor;
            connectionLine.startColor = lineColor;
            connectionLine.endColor = lineColor;
        }

        OnCellSelected?.Invoke(nearestCell);
    }

    public void SetNearestCell(GridCell cell)
    {
        nearestCell = cell;
    }

    public void EndDrag(Vector2 worldPos)
    {
        if (!isDragging) return;

        if (nearestCell != null)
        {
            OnCellDropped?.Invoke(nearestCell);
        }

        StopDrag();
    }

    public void StopDrag()
    {
        isDragging = false;
        currentCell = null;
        nearestCell = null;

        if (dragIndicator != null)
        {
            dragIndicator.SetActive(false);
        }

        if (connectionLine != null)
        {
            connectionLine.enabled = false;
        }

        OnDragStopped?.Invoke();
    }
}
