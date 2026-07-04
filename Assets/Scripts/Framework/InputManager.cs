using UnityEngine;
using System;

/// <summary>
/// 输入管理器 - 处理触摸和鼠标输入
/// </summary>
public class InputManager : Singleton<InputManager>
{
    public event Action<Vector2> OnClickDown;
    public event Action<Vector2> OnClickHold;
    public event Action<Vector2> OnClickUp;

    private bool isDragging = false;

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
            return;

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
