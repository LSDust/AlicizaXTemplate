# Localization 模块

Localization 模块提供运行时语言管理、本地化表加载、字符串查询、格式化字符串、语言切换事件和语言偏好保存。配置数据由 `GameLocaizationTable` 和 `LocalizationLanguage` 两类 ScriptableObject 组成。

源码位置：

- `Client/Packages/com.alicizax.unity.framework/Runtime/Localization`

## 使用前提

场景中的框架根节点需要挂载：

- `LocalizationComponent`

`LocalizationComponent` 会注册 `ILocalizationService`，读取上次保存的语言偏好，并调用 `Initialize(language)`。默认语言为 `ChineseSimplified`。

```csharp
using AlicizaX;
using AlicizaX.Localization.Runtime;

ILocalizationService localization = AppServices.Require<ILocalizationService>();
```

也可以使用快捷入口：

```csharp
ILocalizationService localization = GameApp.Localization;
```

## 配置语言表

运行时数据结构：

- `GameLocaizationTable`：一张本地化表，包含多个语言资源。
- `LocalizationLanguage`：一种语言，包含 `LanguageName` 和字符串列表。
- `LocalizationString`：单条文本，包含 `Key` 和 `Value`。

语言名要与切换语言时传入的字符串一致，例如：

```text
ChineseSimplified
English
Japanese
```

## 加载本地化表

```csharp
using AlicizaX;
using AlicizaX.Localization;
using AlicizaX.Localization.Runtime;
using AlicizaX.Resource.Runtime;
using UnityEngine;

public sealed class LocalizationLoadExample : MonoBehaviour
{
    private GameLocaizationTable _table;

    private void Start()
    {
        IResourceService resources = GameApp.Resource;
        _table = resources.LoadAsset<GameLocaizationTable>(
            "Assets/Bundles/Localization/GameLocaizationTable.asset");

        GameApp.Localization.CoverAddLocalizationConfig(_table);
    }

    private void OnDestroy()
    {
        if (_table != null)
        {
            GameApp.Resource.UnloadAsset(_table);
            _table = null;
        }
    }
}
```

常用加载方式：

- `CoverAddLocalizationConfig(table)`：清空当前文本后加载该表，适合作为主表初始化。
- `IncreAddLocalizationConfig(table)`：增量追加该表，适合 DLC、活动、玩法模块追加文本。
- `ReloadLocalizationConfig(table)`：重新加载某张已跟踪表，适合热更新后刷新表内容。

这些接口都会跟踪表对象。切换语言时，服务会按当前语言重新应用所有已跟踪表。

## 获取文本

```csharp
string title = GameApp.Localization.GetString("ui.login.title");
string count = GameApp.Localization.GetString("item.count", 12);
string reward = GameApp.Localization.GetString("reward.desc", "Gold", 100);
```

如果 key 不存在，`GetString` 会返回 key 本身，便于在界面上直接发现缺失文本。

需要区分是否真的存在 key 时使用：

```csharp
if (GameApp.Localization.TryGetRawString("ui.login.title", out string value))
{
    Debug.Log(value);
}
```

## 切换语言

```csharp
using AlicizaX;
using Cysharp.Threading.Tasks;

public sealed class LanguageSwitchExample
{
    public async UniTask SwitchToEnglish()
    {
        await GameApp.Localization.SwitchLanguageAsync("English");
    }
}
```

如果不需要等待结果，也可以使用：

```csharp
GameApp.Localization.ChangedLanguage("English");
```

切换成功后会保存语言偏好，并发布 `LocalizationChangeEvent`。

## 监听语言切换

```csharp
using AlicizaX;
using AlicizaX.Localization;
using UnityEngine;

public sealed class LocalizationRefreshView : MonoBehaviour
{
    private EventRuntimeHandle _handle;

    private void OnEnable()
    {
        _handle = EventBus.Subscribe<LocalizationChangeEvent>(OnLanguageChanged);
    }

    private void OnDisable()
    {
        _handle.Dispose();
    }

    private void OnLanguageChanged(in LocalizationChangeEvent evt)
    {
        Debug.Log($"Language changed: {evt.ChangedLanguage}");
        RefreshText();
    }

    private void RefreshText()
    {
        // 重新从 GameApp.Localization 获取文本并刷新界面。
    }
}
```

## UI 中刷新文本

```csharp
using AlicizaX;
using AlicizaX.Localization;
using AlicizaX.UI.Runtime;
using Game.UI;

public sealed class LoginWindow : UIWindow<ui_LoginWindow>
{
    protected override void OnInitialize()
    {
        RefreshText();
    }

    protected override void OnRegisterEvent(EventListenerProxy proxy)
    {
        proxy.AddUIEvent<LocalizationChangeEvent>(OnLanguageChanged);
    }

    private void OnLanguageChanged(in LocalizationChangeEvent evt)
    {
        RefreshText();
    }

    private void RefreshText()
    {
        baseui.TxtTitle.text = GameApp.Localization.GetString("ui.login.title");
    }
}
```

UI 使用 `EventListenerProxy` 后，窗口销毁时事件会自动取消订阅。

## API 速查

| API | 说明 |
| --- | --- |
| `Language` | 当前语言名 |
| `Initialize(language)` | 初始化当前语言 |
| `GetString(key)` | 获取文本，不存在时返回 key |
| `GetString(key, args...)` | 获取并格式化文本 |
| `TryGetRawString(key, out value)` | 尝试获取原始文本 |
| `CoverAddLocalizationConfig(table)` | 清空后加载配置表 |
| `IncreAddLocalizationConfig(table)` | 增量加载配置表 |
| `ReloadLocalizationConfig(table)` | 重新加载配置表 |
| `SwitchLanguageAsync(language)` | 异步切换语言 |
| `ChangedLanguage(language)` | fire-and-forget 切换语言 |
| `LocalizationChangeEvent` | 语言切换完成事件 |

## 注意事项

1. `GameLocaizationTable` 的命名存在源码拼写 `Locaization`，使用时按源码类型名书写。
2. `LocalizationLanguage.LanguageName` 必须与切换语言传入的字符串完全一致。
3. `CoverAddLocalizationConfig` 会清空已加载文本和已跟踪表，适合作为初始化主入口。
4. `SwitchLanguageAsync` 当前实现是同步重建缓存后返回 `UniTask.CompletedTask`，但业务仍推荐按异步接口调用。
5. 缺失 key 返回 key 本身，不会抛异常。

---

## 编辑器静态工具类 LocalizationEditorTools

源码位置：

- `Client/Packages/com.alicizax.unity.framework/Editor/Localization/LocalizationEditorTools.cs`

`LocalizationEditorTools` 是对 Localization 编辑器所有操作的纯静态编程接口，不含任何 GUI 或 MenuItem，适合在自定义编辑器脚本、批处理工具或 AI 辅助流程中调用。

所有写操作会自动调用 `EditorUtility.SetDirty`，批量编辑完成后调用一次 `SaveTable()` 即可持久化。

### 资产发现

```csharp
// 扫描项目中所有 GameLocaizationTable 资产
List<GameLocaizationTable> tables = LocalizationEditorTools.FindAllTables();
```

### 重复 key 检测

```csharp
using AlicizaX.Localization.Editor;

// 检查单张表内的重复 key
var reports = LocalizationEditorTools.FindDuplicateKeys(table);

// 跨所有表检查，支持可选的子字符串过滤（大小写不敏感）
var reports = LocalizationEditorTools.FindDuplicateKeysAcrossTables(filter: "UI");

foreach (var report in reports)
{
    Debug.LogWarning($"重复 key: {report.NormalizedKey}，出现 {report.Occurrences.Count} 次");
    foreach (var occ in report.Occurrences)
        Debug.LogWarning($"  → {occ.Table.name}  sectionId={occ.SectionId}  entryId={occ.EntryId}");
}
```

### 查询

```csharp
// 获取表中所有完整路径 key
List<string> keys = LocalizationEditorTools.GetAllKeys(table);

// 判断 section / key 是否存在
bool hasSection = LocalizationEditorTools.ContainsSection(table, "UI");
bool hasKey     = LocalizationEditorTools.ContainsKey(table, "UI", "Login.Title");

// 读取某条 entry 的翻译值
if (LocalizationEditorTools.TryGetValue(table, "UI", "Login.Title", "ChineseSimplified", out string val))
    Debug.Log(val);
```

### Section 操作

```csharp
// 新增 section（名称去除空格后须为合法 C# 标识符）
if (!LocalizationEditorTools.TryAddSection(table, "Shop", out string err))
    Debug.LogError(err);

// 重命名 section，自动同步所有语言资产的 Key 前缀
LocalizationEditorTools.TryRenameSection(table, "Shop", "Store", out _);

// 删除 section 及其所有 entry
LocalizationEditorTools.TryRemoveSection(table, "Store", out _);
```

### Entry 操作

```csharp
// 新增 entry，自动生成 ID，向所有语言资产写入空值占位
LocalizationEditorTools.TryAddEntry(table, "Shop", "ItemName", out _, isGen: true);

// 设置翻译值
LocalizationEditorTools.TrySetValue(table, "Shop", "ItemName", "ChineseSimplified", "道具名称", out _);
LocalizationEditorTools.TrySetValue(table, "Shop", "ItemName", "English", "Item Name", out _);

// 重命名 entry key，自动同步所有语言资产
LocalizationEditorTools.TryRenameEntry(table, "Shop", "ItemName", "GoodsName", out _);

// 开关代码生成
LocalizationEditorTools.TrySetEntryIsGen(table, "Shop", "GoodsName", true, out _);

// 删除 entry
LocalizationEditorTools.TryRemoveEntry(table, "Shop", "GoodsName", out _);
```

### 批量幂等操作

```csharp
// 不存在则创建，存在则跳过（适合批处理脚本）
LocalizationEditorTools.TryEnsureSection(table, "Achievement", out _);
LocalizationEditorTools.TryEnsureEntry(table, "Achievement", "FirstLogin", out _);
```

### 保存与代码生成

```csharp
// 持久化所有改动并刷新 AssetDatabase
LocalizationEditorTools.SaveTable(table);

// 触发 C# key 类代码生成（等价于编辑器 Gen Code 按钮）
LocalizationEditorTools.GenerateCode(table);
```

### API 速查

| API | 说明 |
| --- | --- |
| `FindAllTables()` | 扫描项目中所有本地化表资产 |
| `FindDuplicateKeys(table, filter?)` | 单表内重复 key 检测 |
| `FindDuplicateKeysAcrossTables(tables?, filter?)` | 跨表重复 key 检测 |
| `GetAllKeys(table)` | 返回所有完整路径 key |
| `ContainsSection(table, section)` | 判断 section 是否存在 |
| `ContainsKey(table, section, key)` | 判断 key 是否存在 |
| `TryGetValue(table, section, key, lang, out value)` | 读取翻译值 |
| `TryAddSection(table, name, out error)` | 新增 section |
| `TryRemoveSection(table, name, out error)` | 删除 section |
| `TryRenameSection(table, old, new, out error)` | 重命名 section |
| `TryAddEntry(table, section, key, out error, isGen?)` | 新增 entry |
| `TryRemoveEntry(table, section, key, out error)` | 删除 entry |
| `TryRenameEntry(table, section, old, new, out error)` | 重命名 entry |
| `TrySetValue(table, section, key, lang, value, out error)` | 设置翻译值 |
| `TrySetEntryIsGen(table, section, key, isGen, out error)` | 开关代码生成 |
| `TryEnsureSection(table, name, out error)` | 幂等确保 section 存在 |
| `TryEnsureEntry(table, section, key, out error, isGen?)` | 幂等确保 entry 存在 |
| `SaveTable(table, refreshAssets?)` | 持久化并刷新资产数据库 |
| `GenerateCode(table)` | 触发 C# key 类代码生成 |
