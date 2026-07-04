using UnityEngine;
using System;

public class AnchorDragHandler : MonoBehaviour
{
    public static AnchorDragHandler Instance { get; private set; }

    [Header("Drag Settings")]
    public float snapDistance = 0.5f;
    public float dragSpeed = 10f;

    [Header("Visual Feedback")]
    public GameObject dragIndicator;
    public LineRenderer connectionLine;
    public Color validColor = Color.green;
    public Color invalidColor = Color.red;

    public event Action<AnchorSlot> OnAnchorPlaced;
    public event Action OnAnchorRemoved;

    private bool isDragging = false;
    private AnchorSlot currentSlot;
    private AnchorSlot nearestSlot;
    private Vector2 dragOffset;

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

    private void OnEnable()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnClickDown += HandleClickDown;
            InputManager.Instance.OnClickHold += HandleClickHold;
            InputManager.Instance.OnClickUp += HandleClickUp;
        }
    }

    private void OnDisable()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnClickDown -= HandleClickDown;
            InputManager.Instance.OnClickHold -= HandleClickHold;
            InputManager.Instance.OnClickUp -= HandleClickUp;
        }
    }

    private void HandleClickDown(Vector2 screenPos)
    {
        Vector2 worldPos = Camera.main.ScreenToWorldPoint(screenPos);
        AnchorSlot slot = GridManager.Instance.GetNearestSlot(worldPos);

        if (slot != null && !slot.IsOccupied && slot.IsValid)
        {
            StartDragging(slot, worldPos);
        }
        else if (slot != null && slot.IsOccupied)
        {
            RemoveAnchorFromSlot(slot);
        }
    }

    private void HandleClickHold(Vector2 screenPos)
    {
        if (!isDragging) return;

        Vector2 worldPos = Camera.main.ScreenToWorldPoint(screenPos);
        UpdateDragPosition(worldPos);
    }

    private void HandleClickUp(Vector2 screenPos)
    {
        if (!isDragging) return;

        Vector2 worldPos = Camera.main.ScreenToWorldPoint(screenPos);
        FinishDrag(worldPos);
    }

    private void StartDragging(AnchorSlot slot, Vector2 worldPos)
    {
        isDragging = true;
        currentSlot = slot;
        nearestSlot = slot;

        if (dragIndicator != null)
        {
            dragIndicator.SetActive(true);
            dragIndicator.transform.position = worldPos;
        }

        if (connectionLine != null)
        {
            connectionLine.enabled = true;
            connectionLine.SetPosition(0, slot.transform.position);
            connectionLine.SetPosition(1, worldPos);
        }

        GridManager.Instance.HighlightValidSlots(true);
    }

    private void UpdateDragPosition(Vector2 worldPos)
    {
        if (dragIndicator != null)
        {
            dragIndicator.transform.position = worldPos;
        }

        nearestSlot = GridManager.Instance.GetNearestSlot(worldPos);

        if (connectionLine != null && currentSlot != null)
        {
            connectionLine.SetPosition(1, worldPos);

            float distance = Vector2.Distance(currentSlot.transform.position, worldPos);
            Color lineColor = distance <= snapDistance ? validColor : invalidColor;
            connectionLine.startColor = lineColor;
            connectionLine.endColor = lineColor;
        }

        foreach (var slot in GridManager.Instance.GetAllSlots())
        {
            if (slot != null && !slot.IsOccupied)
            {
                bool isNearest = slot == nearestSlot;
                slot.SetHighlighted(isNearest);
            }
        }
    }

    private void FinishDrag(Vector2 worldPos)
    {
        if (nearestSlot != null && nearestSlot.IsValid && !nearestSlot.IsOccupied)
        {
            PlaceAnchorOnSlot(nearestSlot);
        }

        StopDragging();
    }

    private void PlaceAnchorOnSlot(AnchorSlot slot)
    {
        if (slot.PlaceAnchor())
        {
            OnAnchorPlaced?.Invoke(slot);
        }
    }

    private void RemoveAnchorFromSlot(AnchorSlot slot)
    {
        if (slot.RemoveAnchor())
        {
            OnAnchorRemoved?.Invoke();
        }
    }

    private void StopDragging()
    {
        isDragging = false;
        currentSlot = null;
        nearestSlot = null;

        if (dragIndicator != null)
        {
            dragIndicator.SetActive(false);
        }

        if (connectionLine != null)
        {
            connectionLine.enabled = false;
        }

        GridManager.Instance.HighlightValidSlots(false);
    }

    public bool IsDragging()
    {
        return isDragging;
    }
}
