# Resources 文件夹结构

## 推荐目录结构

```
Assets/Resources/
├── UI/
│   ├── Panels/        # UI面板预制体
│   ├── Prefabs/       # 其他UI预制体
│   └── Sprites/       # UI图片
├── Game/
│   ├── Prefabs/       # 游戏预制体
│   ├── Sprites/       # 游戏图片
│   └── Effects/       # 特效预制体
├── Audio/
│   ├── BGM/           # 背景音乐
│   └── SFX/           # 音效
├── Levels/            # 关卡数据
└── Materials/         # 材质
```

## 使用示例

### 加载预制体
```csharp
// 同步加载
GameObject prefab = ResourceManager.Instance.LoadPrefab("UI/Panels/MainMenuPanel");

// 异步加载
StartCoroutine(ResourceManager.Instance.LoadPrefabAsync("UI/Panels/MainMenuPanel", (prefab) => {
    Instantiate(prefab);
}));

// 直接实例化
GameObject panel = ResourceManager.Instance.Instantiate("UI/Panels/MainMenuPanel", transform);
```

### 加载图片
```csharp
// 加载精灵
Sprite sprite = ResourceManager.Instance.LoadSprite("UI/Sprites/BtnStart");

// 加载纹理
Texture2D tex = ResourceManager.Instance.LoadTexture("Game/Sprites/Player");
```

### 加载音频
```csharp
AudioClip clip = ResourceManager.Instance.LoadAudioClip("Audio/SFX/Click");
GetComponent<AudioSource>().PlayOneShot(clip);
```

### 加载文本
```csharp
string json = ResourceManager.Instance.LoadText("Levels/Level1");
LevelData data = JsonUtility.FromJson<LevelData>(json);
```

## 注意事项

1. Resources 文件夹下的资源会被打包到最终构建中
2. 不要在 Resources 中放置过大的资源
3. 使用缓存可以避免重复加载
4. 适时调用 `UnloadUnusedResources()` 释放内存
