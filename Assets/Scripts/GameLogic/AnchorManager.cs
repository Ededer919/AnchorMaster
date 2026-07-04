using UnityEngine;
using System.Collections.Generic;

public class AnchorManager : MonoBehaviour
{
    public static AnchorManager Instance { get; private set; }

    [Header("Anchor Settings")]
    public int maxAnchors = 4;
    public int currentAnchors = 0;

    [Header("Level Settings")]
    public List<AnchorSlot> validSlots = new List<AnchorSlot>();

    private Dictionary<AnchorSlot, GameObject> placedAnchors = new Dictionary<AnchorSlot, GameObject>();

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
        if (AnchorDragHandler.Instance != null)
        {
            AnchorDragHandler.Instance.OnAnchorPlaced += HandleAnchorPlaced;
            AnchorDragHandler.Instance.OnAnchorRemoved += HandleAnchorRemoved;
        }
    }

    private void OnDisable()
    {
        if (AnchorDragHandler.Instance != null)
        {
            AnchorDragHandler.Instance.OnAnchorPlaced -= HandleAnchorPlaced;
            AnchorDragHandler.Instance.OnAnchorRemoved -= HandleAnchorRemoved;
        }
    }

    private void Start()
    {
        InitializeAnchors();
    }

    public void InitializeAnchors()
    {
        currentAnchors = 0;
        placedAnchors.Clear();

        foreach (var slot in validSlots)
        {
            if (slot != null)
            {
                slot.IsValid = true;
                slot.RemoveAnchor();
            }
        }

        UIManager.Instance?.UpdateAnchorCount(currentAnchors, maxAnchors);
    }

    private void HandleAnchorPlaced(AnchorSlot slot)
    {
        if (currentAnchors >= maxAnchors)
        {
            slot.RemoveAnchor();
            return;
        }

        currentAnchors++;
        placedAnchors[slot] = slot.GetCurrentAnchor();

        UIManager.Instance?.UpdateAnchorCount(currentAnchors, maxAnchors);

        AnchorVisual visual = slot.GetCurrentAnchor()?.GetComponent<AnchorVisual>();
        if (visual != null)
        {
            visual.PlayPlaceAnimation();
        }

        if (currentAnchors >= maxAnchors)
        {
            EnableConfirmButton();
        }
    }

    private void HandleAnchorRemoved()
    {
        currentAnchors--;
        UIManager.Instance?.UpdateAnchorCount(currentAnchors, maxAnchors);

        if (currentAnchors < maxAnchors)
        {
            DisableConfirmButton();
        }
    }

    public void RemoveAllAnchors()
    {
        foreach (var kvp in placedAnchors)
        {
            kvp.Key.RemoveAnchor();
        }

        placedAnchors.Clear();
        currentAnchors = 0;

        UIManager.Instance?.UpdateAnchorCount(currentAnchors, maxAnchors);
        DisableConfirmButton();
    }

    private void EnableConfirmButton()
    {
        UIManager.Instance?.EnableConfirmButton(true);
    }

    private void DisableConfirmButton()
    {
        UIManager.Instance?.EnableConfirmButton(false);
    }

    public int GetRemainingAnchors()
    {
        return maxAnchors - currentAnchors;
    }

    public bool CanPlaceAnchor()
    {
        return currentAnchors < maxAnchors;
    }

    public Dictionary<AnchorSlot, GameObject> GetPlacedAnchors()
    {
        return placedAnchors;
    }
}
