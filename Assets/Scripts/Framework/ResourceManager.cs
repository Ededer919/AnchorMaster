using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 资源管理器 - 从 Resources 文件夹加载资源
/// 支持缓存、异步加载、预加载
/// </summary>
public class ResourceManager : Singleton<ResourceManager>
{
    [Header("Settings")]
    public bool enableCache = true;
    public int maxCacheSize = 100;

    // 资源缓存
    private Dictionary<string, Object> resourceCache = new Dictionary<string, Object>();
    private Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();
    private Dictionary<string, GameObject> prefabCache = new Dictionary<string, GameObject>();

    // ========== 同步加载 ==========

    /// <summary>
    /// 加载资源
    /// </summary>
    public T Load<T>(string path) where T : Object
    {
        if (enableCache && resourceCache.ContainsKey(path))
        {
            return resourceCache[path] as T;
        }

        T resource = Resources.Load<T>(path);

        if (resource != null && enableCache)
        {
            resourceCache[path] = resource;
        }

        return resource;
    }

    /// <summary>
    /// 加载预制体
    /// </summary>
    public GameObject LoadPrefab(string path)
    {
        if (enableCache && prefabCache.ContainsKey(path))
        {
            return prefabCache[path];
        }

        GameObject prefab = Resources.Load<GameObject>(path);

        if (prefab != null && enableCache)
        {
            prefabCache[path] = prefab;
        }

        return prefab;
    }

    /// <summary>
    /// 加载精灵
    /// </summary>
    public Sprite LoadSprite(string path)
    {
        if (enableCache && spriteCache.ContainsKey(path))
        {
            return spriteCache[path];
        }

        Sprite sprite = Resources.Load<Sprite>(path);

        if (sprite != null && enableCache)
        {
            spriteCache[path] = sprite;
        }

        return sprite;
    }

    /// <summary>
    /// 加载纹理
    /// </summary>
    public Texture2D LoadTexture(string path)
    {
        return Load<Texture2D>(path);
    }

    /// <summary>
    /// 加载音频剪辑
    /// </summary>
    public AudioClip LoadAudioClip(string path)
    {
        return Load<AudioClip>(path);
    }

    /// <summary>
    /// 加载文本文件
    /// </summary>
    public string LoadText(string path)
    {
        TextAsset textAsset = Load<TextAsset>(path);
        return textAsset != null ? textAsset.text : null;
    }

    /// <summary>
    /// 加载材质
    /// </summary>
    public Material LoadMaterial(string path)
    {
        return Load<Material>(path);
    }

    /// <summary>
    /// 加载动画控制器
    /// </summary>
    public RuntimeAnimatorController LoadAnimatorController(string path)
    {
        return Load<RuntimeAnimatorController>(path);
    }

    // ========== 异步加载 ==========

    /// <summary>
    /// 异步加载资源
    /// </summary>
    public IEnumerator LoadAsync<T>(string path, System.Action<T> onComplete) where T : Object
    {
        ResourceRequest request = Resources.LoadAsync<T>(path);

        yield return request;

        T resource = request.asset as T;

        if (resource != null && enableCache)
        {
            resourceCache[path] = resource;
        }

        onComplete?.Invoke(resource);
    }

    /// <summary>
    /// 异步加载预制体
    /// </summary>
    public IEnumerator LoadPrefabAsync(string path, System.Action<GameObject> onComplete)
    {
        ResourceRequest request = Resources.LoadAsync<GameObject>(path);

        yield return request;

        GameObject prefab = request.asset as GameObject;

        if (prefab != null && enableCache)
        {
            prefabCache[path] = prefab;
        }

        onComplete?.Invoke(prefab);
    }

    /// <summary>
    /// 异步加载精灵
    /// </summary>
    public IEnumerator LoadSpriteAsync(string path, System.Action<Sprite> onComplete)
    {
        ResourceRequest request = Resources.LoadAsync<Sprite>(path);

        yield return request;

        Sprite sprite = request.asset as Sprite;

        if (sprite != null && enableCache)
        {
            spriteCache[path] = sprite;
        }

        onComplete?.Invoke(sprite);
    }

    // ========== 实例化 ==========

    /// <summary>
    /// 实例化预制体
    /// </summary>
    public GameObject Instantiate(string path, Transform parent = null)
    {
        GameObject prefab = LoadPrefab(path);

        if (prefab == null)
        {
            Debug.LogError($"[ResourceManager] 无法加载预制体: {path}");
            return null;
        }

        GameObject instance = Instantiate(prefab, parent);
        instance.name = prefab.name;

        return instance;
    }

    /// <summary>
    /// 异步实例化预制体
    /// </summary>
    public IEnumerator InstantiateAsync(string path, Transform parent, System.Action<GameObject> onComplete)
    {
        ResourceRequest request = Resources.LoadAsync<GameObject>(path);

        yield return request;

        GameObject prefab = request.asset as GameObject;

        if (prefab == null)
        {
            Debug.LogError($"[ResourceManager] 无法加载预制体: {path}");
            onComplete?.Invoke(null);
            yield break;
        }

        GameObject instance = Instantiate(prefab, parent);
        instance.name = prefab.name;

        onComplete?.Invoke(instance);
    }

    // ========== 预加载 ==========

    /// <summary>
    /// 预加载资源列表
    /// </summary>
    public void Preload(string[] paths)
    {
        foreach (string path in paths)
        {
            Object resource = Resources.Load<Object>(path);
            if (resource != null && enableCache)
            {
                resourceCache[path] = resource;
            }
        }

        Debug.Log($"[ResourceManager] 预加载了 {paths.Length} 个资源");
    }

    /// <summary>
    /// 异步预加载资源列表
    /// </summary>
    public IEnumerator PreloadAsync(string[] paths, System.Action<float> onProgress = null)
    {
        for (int i = 0; i < paths.Length; i++)
        {
            ResourceRequest request = Resources.LoadAsync<Object>(paths[i]);
            yield return request;

            if (request.asset != null && enableCache)
            {
                resourceCache[paths[i]] = request.asset;
            }

            onProgress?.Invoke((float)(i + 1) / paths.Length);
        }

        Debug.Log($"[ResourceManager] 预加载了 {paths.Length} 个资源");
    }

    // ========== 缓存管理 ==========

    /// <summary>
    /// 清除所有缓存
    /// </summary>
    public void ClearCache()
    {
        resourceCache.Clear();
        spriteCache.Clear();
        prefabCache.Clear();

        Debug.Log("[ResourceManager] 缓存已清除");
    }

    /// <summary>
    /// 清除指定缓存
    /// </summary>
    public void ClearCache(string path)
    {
        resourceCache.Remove(path);
        spriteCache.Remove(path);
        prefabCache.Remove(path);
    }

    /// <summary>
    /// 获取缓存数量
    /// </summary>
    public int GetCacheCount()
    {
        return resourceCache.Count + spriteCache.Count + prefabCache.Count;
    }

    /// <summary>
    /// 卸载未使用的资源
    /// </summary>
    public void UnloadUnusedResources()
    {
        Resources.UnloadUnusedAssets();
        System.GC.Collect();

        Debug.Log("[ResourceManager] 已卸载未使用的资源");
    }
}

// ========== 资源路径常量 ==========

/// <summary>
/// 资源路径常量
/// </summary>
public static class ResourcePaths
{
    // UI
    public const string UI_PANELS = "UI/Panels";
    public const string UI_PREFABS = "UI/Prefabs";
    public const string UI_SPRITES = "UI/Sprites";

    // 游戏
    public const string GAME_PREFABS = "Game/Prefabs";
    public const string GAME_SPRITES = "Game/Sprites";
    public const string GAME_EFFECTS = "Game/Effects";

    // 音频
    public const string AUDIO_BGM = "Audio/BGM";
    public const string AUDIO_SFX = "Audio/SFX";

    // 关卡
    public const string LEVELS = "Levels";
}
