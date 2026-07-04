using UnityEngine;
using System;

/// <summary>
/// 计时器类 - 可复用的计时器工具
/// </summary>
[Serializable]
public class Timer
{
    public float Duration { get; private set; }
    public float ElapsedTime { get; private set; }
    public float RemainingTime => Mathf.Max(0, Duration - ElapsedTime);
    public bool IsRunning { get; private set; }
    public bool IsFinished => ElapsedTime >= Duration;

    public event Action OnComplete;
    public event Action<float> OnTick;

    private bool useRealTime;

    /// <summary>
    /// 创建计时器
    /// </summary>
    /// <param name="duration">持续时间（秒）</param>
    /// <param name="useRealTime">是否使用真实时间（不受Time.timeScale影响）</param>
    public Timer(float duration, bool useRealTime = false)
    {
        Duration = duration;
        this.useRealTime = useRealTime;
    }

    /// <summary>
    /// 开始计时
    /// </summary>
    public void Start()
    {
        ElapsedTime = 0f;
        IsRunning = true;
    }

    /// <summary>
    /// 停止计时
    /// </summary>
    public void Stop()
    {
        IsRunning = false;
    }

    /// <summary>
    /// 暂停计时
    /// </summary>
    public void Pause()
    {
        IsRunning = false;
    }

    /// <summary>
    /// 恢复计时
    /// </summary>
    public void Resume()
    {
        IsRunning = true;
    }

    /// <summary>
    /// 重置计时器
    /// </summary>
    public void Reset()
    {
        ElapsedTime = 0f;
        IsRunning = false;
    }

    /// <summary>
    /// 更新计时器（需要在Update中调用）
    /// </summary>
    public void Update()
    {
        if (!IsRunning) return;

        float deltaTime = useRealTime ? Time.unscaledDeltaTime : Time.deltaTime;
        ElapsedTime += deltaTime;

        OnTick?.Invoke(ElapsedTime);

        if (ElapsedTime >= Duration)
        {
            IsRunning = false;
            OnComplete?.Invoke();
        }
    }

    /// <summary>
    /// 获取进度（0-1）
    /// </summary>
    public float GetProgress()
    {
        if (Duration <= 0) return 1f;
        return Mathf.Clamp01(ElapsedTime / Duration);
    }

    /// <summary>
    /// 格式化时间显示
    /// </summary>
    public string FormatTime(float time)
    {
        int minutes = (int)(time / 60);
        int seconds = (int)(time % 60);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    /// <summary>
    /// 获取格式化的剩余时间
    /// </summary>
    public string GetFormattedRemainingTime()
    {
        return FormatTime(RemainingTime);
    }

    /// <summary>
    /// 获取格式化的已用时间
    /// </summary>
    public string GetFormattedElapsedTime()
    {
        return FormatTime(ElapsedTime);
    }
}

/// <summary>
/// 计时器管理器组件
/// </summary>
public class TimerManager : Singleton<TimerManager>
{
    private System.Collections.Generic.List<Timer> timers = new System.Collections.Generic.List<Timer>();

    /// <summary>
    /// 创建并启动一个计时器
    /// </summary>
    public Timer CreateTimer(float duration, Action onComplete = null, bool useRealTime = false)
    {
        Timer timer = new Timer(duration, useRealTime);
        if (onComplete != null)
        {
            timer.OnComplete += onComplete;
        }
        timers.Add(timer);
        timer.Start();
        return timer;
    }

    /// <summary>
    /// 移除计时器
    /// </summary>
    public void RemoveTimer(Timer timer)
    {
        timers.Remove(timer);
    }

    private void Update()
    {
        for (int i = timers.Count - 1; i >= 0; i--)
        {
            timers[i].Update();
            if (timers[i].IsFinished)
            {
                timers.RemoveAt(i);
            }
        }
    }
}
