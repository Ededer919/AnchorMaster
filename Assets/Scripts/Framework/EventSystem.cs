using System;
using System.Collections.Generic;

/// <summary>
/// 轻量级事件系统 - 发布/订阅模式
/// 用法: EventSystem.Instance.Subscribe<GameStartEvent>(OnGameStart);
/// </summary>
public class EventSystem
{
    private static EventSystem _instance;
    public static EventSystem Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new EventSystem();
            }
            return _instance;
        }
    }

    private readonly Dictionary<Type, List<Delegate>> eventHandlers = new Dictionary<Type, List<Delegate>>();

    /// <summary>
    /// 订阅事件
    /// </summary>
    public void Subscribe<T>(Action<T> handler) where T : struct
    {
        Type eventType = typeof(T);

        if (!eventHandlers.ContainsKey(eventType))
        {
            eventHandlers[eventType] = new List<Delegate>();
        }

        if (!eventHandlers[eventType].Contains(handler))
        {
            eventHandlers[eventType].Add(handler);
        }
    }

    /// <summary>
    /// 取消订阅
    /// </summary>
    public void Unsubscribe<T>(Action<T> handler) where T : struct
    {
        Type eventType = typeof(T);

        if (eventHandlers.ContainsKey(eventType))
        {
            eventHandlers[eventType].Remove(handler);
        }
    }

    /// <summary>
    /// 发布事件
    /// </summary>
    public void Publish<T>(T eventData) where T : struct
    {
        Type eventType = typeof(T);

        if (eventHandlers.ContainsKey(eventType))
        {
            // 复制列表避免在遍历中修改
            List<Delegate> handlers = new List<Delegate>(eventHandlers[eventType]);

            foreach (var handler in handlers)
            {
                if (handler is Action<T> typedHandler)
                {
                    typedHandler.Invoke(eventData);
                }
            }
        }
    }

    /// <summary>
    /// 清除所有订阅
    /// </summary>
    public void Clear()
    {
        eventHandlers.Clear();
    }

    /// <summary>
    /// 清除指定事件的所有订阅
    /// </summary>
    public void Clear<T>() where T : struct
    {
        Type eventType = typeof(T);
        if (eventHandlers.ContainsKey(eventType))
        {
            eventHandlers[eventType].Clear();
        }
    }
}

// ========== 预定义事件 ==========

/// <summary>
/// 游戏状态改变事件
/// </summary>
public struct GameStateChangedEvent
{
    public GameState NewState;
    public GameState OldState;
}

/// <summary>
/// 游戏开始事件
/// </summary>
public struct GameStartEvent { }

/// <summary>
/// 游戏暂停事件
/// </summary>
public struct GamePauseEvent
{
    public bool IsPaused;
}

/// <summary>
/// 游戏结束事件
/// </summary>
public struct GameOverEvent
{
    public bool IsWin;
    public int FinalScore;
}

/// <summary>
/// 分数改变事件
/// </summary>
public struct ScoreChangedEvent
{
    public int OldScore;
    public int NewScore;
}

/// <summary>
/// 关卡加载事件
/// </summary>
public struct LevelLoadedEvent
{
    public int LevelIndex;
}

/// <summary>
/// 风力改变事件
/// </summary>
public struct WindChangedEvent
{
    public UnityEngine.Vector2 WindDirection;
    public float WindStrength;
}
