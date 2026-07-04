using UnityEngine;
using System;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    public event Action<Vector2> OnClickDown;
    public event Action<Vector2> OnClickHold;
    public event Action<Vector2> OnClickUp;

    private bool isDragging = false;

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

    private void Update()
    {
        Vector2 inputPos = GetInputPosition();

        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
            OnClickDown?.Invoke(inputPos);
        }

        if (Input.GetMouseButton(0) && isDragging)
        {
            OnClickHold?.Invoke(inputPos);
        }

        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            isDragging = false;
            OnClickUp?.Invoke(inputPos);
        }
    }

    private Vector2 GetInputPosition()
    {
        if (Input.touchCount > 0)
        {
            return Input.GetTouch(0).position;
        }
        return Input.mousePosition;
    }

    public bool IsDragging()
    {
        return isDragging;
    }
}
