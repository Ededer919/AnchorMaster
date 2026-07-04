# GameLaunch 启动框架

## 文件说明

| 文件 | 说明 |
|------|------|
| `GameLaunch.cs` | 游戏启动器，负责初始化所有框架组件 |
| `GameConfig.cs` | 游戏配置（ScriptableObject），存储启动参数 |

## 使用方法

### 1. 场景设置

在场景中创建一个空 GameObject，命名为 `GameLaunch`，挂载 `GameLaunch` 脚本。

### 2. 配置参数

在 Inspector 中配置：

| 参数 | 说明 | 默认值 |
|------|------|--------|
| Create GameManager | 是否创建游戏管理器 | true |
| Create UI Manager | 是否创建UI管理器 | true |
| Create Input Manager | 是否创建输入管理器 | true |
| Create Timer Manager | 是否创建计时器管理器 | true |
| Grid Size | 网格大小 | 4 |
| Start Level | 起始关卡 | 1 |

### 3. 可选：创建游戏配置

1. 右键 -> Create -> Game -> Config
2. 配置游戏参数
3. 将配置赋值给 GameLaunch（需要扩展代码支持）

## 启动流程

```
GameLaunch.Awake()
    ↓
GameLaunch.Start()
    ↓
InitializeManagers()
    ├── EnsureManager<GameManager>()
    ├── EnsureManager<UIManager>()
    ├── EnsureManager<InputManager>()
    └── EnsureManager<TimerManager>()
    ↓
InitializeGame()
    ├── 设置游戏参数
    └── SubscribeToEvents()
    ↓
游戏开始运行
```

## 事件订阅

GameLaunch 会自动订阅以下事件：

- `GameStartEvent` - 游戏开始
- `GameOverEvent` - 游戏结束
- `GameStateChangedEvent` - 状态变化

## 扩展

如需添加新的管理器，在 `InitializeManagers()` 中添加：

```csharp
if (createNewManager)
{
    EnsureManager<NewManager>();
}
```

如需监听更多事件，在 `SubscribeToEvents()` 中添加订阅。
