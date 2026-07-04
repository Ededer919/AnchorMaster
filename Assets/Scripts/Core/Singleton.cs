using UnityEngine;

/// <summary>
/// 泛型单例基类
/// 用法: public class MyManager : Singleton<MyManager> { }
/// </summary>
public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    public static T Instance { get; private set; }

    [Header("Singleton Settings")]
    public bool persistAcrossScenes = true;

    protected virtual void Awake()
    {
        if (Instance == null)
        {
            Instance = this as T;
            if (persistAcrossScenes)
            {
                DontDestroyOnLoad(gameObject);
            }
        }
        else
        {
            if (Instance != this)
            {
                Destroy(gameObject);
            }
        }
    }

    protected virtual void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
