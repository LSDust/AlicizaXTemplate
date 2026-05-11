# RecyclerView 列表

`RecyclerView` 是扩展包里的虚拟滚动列表组件，负责列表项复用、滚动、布局、焦点导航和局部刷新。业务层通常不直接操作 `RecyclerView.SetAdapter`，而是通过 `UGList`、`UGMixedList`、`UGLoopList`、`UGGroupList` 包装类使用。

源码位置：

- `Client/Packages/com.alicizax.unity.ui.extension/Runtime/RecyclerView`
- 项目示例：`Client/Assets/Scripts/Hotfix/GameLogic/UI/UILoadUpdate.cs`

## 创建列表

推荐在 Unity 菜单中创建：

```text
GameObject/UI/UXScrollView
```

生成的列表结构需要包含：

| 对象 | 说明 |
| --- | --- |
| `RecyclerView` | 挂在滚动区域根节点上 |
| `Content` | 承载列表项实例的 `RectTransform` |
| `Templates` | 一个或多个列表项模板，模板上必须挂 `ViewHolder` 子类 |
| `LayoutManager` | 线性、网格、分页、混合等布局管理器 |
| `Scroller` | 处理拖拽、滚轮和平滑滚动 |
| `Scrollbar` | 可选，显示滚动条 |

在 UI Holder 生成规则中，常见映射为：

```text
ScrollView -> AlicizaX.UI.RecyclerView
```

Prefab 节点命名示例：

```text
ScrollView@ItemList
```

生成后可在窗口中通过 `baseui.ScrollViewItemList` 访问。

## 普通列表

数据类型实现 `ISimpleViewData`：

```csharp
using AlicizaX.UI;

public sealed class BagItemData : ISimpleViewData
{
    public int ItemId;
    public string Name;
    public int Count;
}
```

列表项模板挂一个 `ViewHolder` 子类：

```csharp
using AlicizaX.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class BagItemHolder : ViewHolder
{
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI countText;

    public Image Icon => icon;
    public TextMeshProUGUI NameText => nameText;
    public TextMeshProUGUI CountText => countText;
}
```

渲染逻辑继承 `ItemRender<TData, THolder>`：

```csharp
using AlicizaX;
using AlicizaX.UI;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class BagItemRender : ItemRender<BagItemData, BagItemHolder>
{
    public override ItemInteractionFlags InteractionFlags => ItemInteractionFlags.PointerNavigation;

    protected override void OnBind(BagItemData data, int index)
    {
        baseui.NameText.text = data.Name;
        baseui.CountText.text = data.Count.ToString();
    }

    protected override void OnPointerClick(PointerEventData eventData)
    {
        Log.Info($"Click item: {CurrentData.ItemId}");
    }

    protected override void OnSelectionChanged(bool selected)
    {
        baseui.Icon.color = selected ? Color.cyan : Color.white;
    }

    protected override void OnClear()
    {
        baseui.Icon.color = Color.white;
    }
}
```

窗口中创建并绑定列表：

```csharp
using System.Collections.Generic;
using AlicizaX.UI;
using AlicizaX.UI.Runtime;
using Game.UI;

[Window(UILayer.UI)]
public sealed class BagWindow : UIWindow<ui_BagWindow>
{
    private UGList<BagItemData> _items;

    protected override void OnInitialize()
    {
        _items = UGListCreateHelper.Create<BagItemData>(baseui.ScrollViewItemList);
        _items.RegisterItemRender<BagItemRender>();
        _items.Data = CreateItems();
    }

    private static List<BagItemData> CreateItems()
    {
        return new List<BagItemData>
        {
            new BagItemData { ItemId = 1001, Name = "Potion", Count = 5 },
            new BagItemData { ItemId = 1002, Name = "Key", Count = 1 },
        };
    }
}
```

## 混合模板列表

一个列表有多种模板时，数据实现 `IMixedViewData`，`TemplateName` 要与模板 `ViewHolder` 类型名一致。

项目示例里使用了同样的写法：

```csharp
using AlicizaX.UI;

public sealed class MailData : IMixedViewData
{
    public string Title;
    public bool HasAttachment;
    public string TemplateName { get; set; }
}
```

创建数据：

```csharp
private static List<MailData> CreateMailList()
{
    return new List<MailData>
    {
        new MailData
        {
            Title = "System Mail",
            HasAttachment = false,
            TemplateName = nameof(MailTextHolder),
        },
        new MailData
        {
            Title = "Reward Mail",
            HasAttachment = true,
            TemplateName = nameof(MailRewardHolder),
        },
    };
}
```

注册多个渲染器：

```csharp
private UGMixedList<MailData> _mails;

protected override void OnInitialize()
{
    _mails = UGListCreateHelper.CreateMixed<MailData>(baseui.ScrollViewMail);
    _mails.RegisterItemRender<MailTextRender>();
    _mails.RegisterItemRender<MailRewardRender>();
    _mails.Data = CreateMailList();
}
```

`ItemRender` 会根据自身泛型里的 `THolder` 自动匹配对应模板。

## 循环列表

`UGLoopList<TData>` 适合轮播、循环选择和无限滚动展示。真实数据数量来自 `GetRealCount()`，显示数量会扩展为循环列表。

```csharp
private UGLoopList<RoleData> _roles;

protected override void OnInitialize()
{
    _roles = UGListCreateHelper.CreateLoop<RoleData>(baseui.ScrollViewRoles);
    _roles.RegisterItemRender<RoleRender>();
    _roles.Data = LoadRoles();
}
```

## 分组列表

`UGGroupList<TData>` 要求数据实现 `IGroupViewData`，并且 `TData` 必须有无参构造函数。它适合任务列表、背包分类、设置页分段、成就分类这类“组头 + 子项”的列表。

分组列表的关键点是：业务传入的 `Data` 里只放真实子项，框架会根据子项的 `Type` 自动创建组头行。也就是说，组头行不是你手动塞进 `Data` 的数据，而是 `GroupAdapter` 在刷新时用 `new TData()` 创建出来的临时显示数据。

`IGroupViewData` 的三个字段含义如下：

| 字段 | 用在组头行 | 用在子项行 |
| --- | --- | --- |
| `Type` | 表示当前组的类型，例如主线、日常、成就 | 表示当前子项属于哪个组 |
| `Expanded` | 记录这个组是否展开 | 一般不用 |
| `TemplateName` | 由 `groupViewName` 自动写入，用来创建组头模板 | 业务自己写入，用来创建子项模板 |

### 数据定义

```csharp
using AlicizaX.UI;

public enum QuestGroupType
{
    Main = 1,
    Daily = 2,
    Achievement = 3,
}

public sealed class QuestRowData : IGroupViewData
{
    public int QuestId;
    public string Title;
    public int Current;
    public int Target;
    public bool Completed;

    public bool Expanded { get; set; }
    public int Type { get; set; }
    public string TemplateName { get; set; }

    public QuestGroupType Group => (QuestGroupType)Type;

    public static QuestRowData Item(
        QuestGroupType group,
        int questId,
        string title,
        int current,
        int target,
        bool completed = false)
    {
        return new QuestRowData
        {
            QuestId = questId,
            Title = title,
            Current = current,
            Target = target,
            Completed = completed,
            Type = (int)group,
            TemplateName = nameof(QuestItemHolder),
        };
    }
}
```

这里的 `TemplateName = nameof(QuestItemHolder)` 很重要。子项行应该指向子项模板，不能写成组头模板名。组头模板名会在创建列表时通过 `groupViewName` 传入。

### 模板 Holder

分组列表通常至少有两个模板：

| 模板 | 作用 |
| --- | --- |
| `QuestGroupHolder` | 组头行，例如“主线任务”“日常任务”，负责显示展开箭头 |
| `QuestItemHolder` | 真实任务行，负责显示任务标题、进度、领取按钮 |

```csharp
using AlicizaX.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class QuestGroupHolder : ViewHolder
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Image arrow;
    [SerializeField] private Image background;

    public TextMeshProUGUI TitleText => titleText;
    public Image Arrow => arrow;
    public Image Background => background;
}

public sealed class QuestItemHolder : ViewHolder
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private Button rewardButton;
    [SerializeField] private Image selectedFrame;

    public TextMeshProUGUI TitleText => titleText;
    public TextMeshProUGUI ProgressText => progressText;
    public Button RewardButton => rewardButton;
    public Image SelectedFrame => selectedFrame;
}
```

Prefab 上需要把这两个模板都放进 `RecyclerView` 的 `Templates` 数组，并且模板根节点分别挂对应的 `ViewHolder` 子类。`ItemRender<TData, THolder>` 会根据 `THolder` 类型名自动匹配模板和渲染器。

### 组头渲染器

组头行是框架创建的临时 `QuestRowData`，默认只有 `Type`、`TemplateName` 和 `Expanded` 是可靠的。组名建议从 `Type` 转换出来，不要依赖 `Title`，因为 `Title` 是子项数据字段，框架创建组头时不会从某个子项复制它。

```csharp
using AlicizaX;
using AlicizaX.UI;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class QuestGroupRender : ItemRender<QuestRowData, QuestGroupHolder>
{
    public override ItemInteractionFlags InteractionFlags => ItemInteractionFlags.PointerNavigation;

    protected override void OnBind(QuestRowData data, int index)
    {
        baseui.TitleText.text = GetGroupTitle(data.Group);
        baseui.Arrow.rectTransform.localEulerAngles = data.Expanded
            ? new Vector3(0f, 0f, 90f)
            : Vector3.zero;
    }

    protected override void OnPointerClick(PointerEventData eventData)
    {
        ToggleGroup();
    }

    protected override void OnSubmit(BaseEventData eventData)
    {
        ToggleGroup();
    }

    protected override void OnSelectionChanged(bool selected)
    {
        baseui.Background.color = selected
            ? new Color(0.25f, 0.45f, 0.65f, 1f)
            : Color.white;
    }

    protected override void OnClear()
    {
        baseui.Background.color = Color.white;
        baseui.Arrow.rectTransform.localEulerAngles = Vector3.zero;
    }

    private void ToggleGroup()
    {
        if (Adapter is GroupAdapter<QuestRowData> adapter)
        {
            adapter.Activate(CurrentIndex);
        }
    }

    private static string GetGroupTitle(QuestGroupType group)
    {
        return group switch
        {
            QuestGroupType.Main => "主线任务",
            QuestGroupType.Daily => "日常任务",
            QuestGroupType.Achievement => "成就任务",
            _ => "其他任务",
        };
    }
}
```

`Adapter.Activate(CurrentIndex)` 在组头行上的行为是切换展开状态；在子项行上的行为是设置选择索引。这里的渲染器只用于组头，所以点击和提交都直接调用它即可。

### 子项渲染器

子项渲染器只处理真实任务数据，`Title`、`Current`、`Target`、`Completed` 这些业务字段都来自你传入的 `Data`。

```csharp
using AlicizaX;
using AlicizaX.UI;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class QuestItemRender : ItemRender<QuestRowData, QuestItemHolder>
{
    public override ItemInteractionFlags InteractionFlags => ItemInteractionFlags.PointerNavigation;

    protected override void OnBind(QuestRowData data, int index)
    {
        baseui.TitleText.text = data.Title;
        baseui.ProgressText.text = $"{data.Current}/{data.Target}";
        baseui.RewardButton.interactable = data.Completed;
    }

    protected override void OnPointerClick(PointerEventData eventData)
    {
        OpenQuestDetail();
    }

    protected override void OnSubmit(BaseEventData eventData)
    {
        OpenQuestDetail();
    }

    protected override void OnSelectionChanged(bool selected)
    {
        baseui.SelectedFrame.gameObject.SetActive(selected);
    }

    protected override void OnClear()
    {
        baseui.SelectedFrame.gameObject.SetActive(false);
        baseui.RewardButton.interactable = false;
    }

    private void OpenQuestDetail()
    {
        Log.Info($"Open quest detail: {CurrentData.QuestId}");
    }
}
```

如果详情打开逻辑属于窗口层，也可以只在 `OnChoiceIndexChanged` 里处理子项选择。点击列表项时框架会先更新 `ChoiceIndex`，再回调 `OnPointerClick`。

### 窗口中创建分组列表

```csharp
using System.Collections.Generic;
using AlicizaX;
using AlicizaX.UI;
using AlicizaX.UI.Runtime;
using Game.UI;

[Window(UILayer.UI)]
public sealed class QuestWindow : UIWindow<ui_QuestWindow>
{
    private UGGroupList<QuestRowData> _quests;

    protected override void OnInitialize()
    {
        _quests = UGListCreateHelper.CreateGroup<QuestRowData>(
            baseui.ScrollViewQuest,
            groupViewName: nameof(QuestGroupHolder));

        _quests.RegisterItemRender<QuestGroupRender>();
        _quests.RegisterItemRender<QuestItemRender>();
        _quests.OnChoiceIndexChanged += OnQuestChoiceChanged;

        _quests.Data = CreateQuestRows();

        ExpandGroupByType(QuestGroupType.Main);
    }

    private void OnQuestChoiceChanged(int displayIndex)
    {
        if (!_quests.TryGetDisplayData(displayIndex, out QuestRowData row))
        {
            return;
        }

        if (_quests.IsGroupIndex(displayIndex))
        {
            return;
        }

        OpenQuestDetail(row.QuestId);
    }

    private void ExpandGroupByType(QuestGroupType group)
    {
        for (int index = 0; _quests.TryGetDisplayData(index, out QuestRowData row); index++)
        {
            if (_quests.IsGroupIndex(index) && row.Type == (int)group)
            {
                _quests.Expand(index);
                return;
            }
        }
    }

    private static List<QuestRowData> CreateQuestRows()
    {
        return new List<QuestRowData>
        {
            QuestRowData.Item(QuestGroupType.Main, 1001, "前往王城", 1, 1, true),
            QuestRowData.Item(QuestGroupType.Main, 1002, "拜访骑士团长", 0, 1),
            QuestRowData.Item(QuestGroupType.Daily, 2001, "完成三次副本", 1, 3),
            QuestRowData.Item(QuestGroupType.Daily, 2002, "赠送一次礼物", 0, 1),
            QuestRowData.Item(QuestGroupType.Achievement, 3001, "累计登录七天", 4, 7),
        };
    }

    private void OpenQuestDetail(int questId)
    {
        Log.Info($"Quest detail: {questId}");
    }
}
```

### 为什么 `CreateGroup` 要传入 `groupViewName`

`CreateGroup<TData>(recyclerView, groupViewName)` 的第二个参数不是显示文本，而是组头模板名。推荐写成：

```csharp
UGListCreateHelper.CreateGroup<QuestRowData>(
    baseui.ScrollViewQuest,
    groupViewName: nameof(QuestGroupHolder));
```

它在内部有两个作用。

第一个作用是选择组头模板。`GroupAdapter` 创建组头行时会执行类似逻辑：

```csharp
TData groupData = new TData
{
    TemplateName = groupViewName,
    Type = type,
};
```

随后 `RecyclerView` 绑定显示行时，会通过 `TemplateName` 找模板。组头行的 `TemplateName` 就是 `QuestGroupHolder`，所以它会使用 `QuestGroupHolder` 模板和 `QuestGroupRender` 渲染器。子项行的 `TemplateName` 是业务数据里写的 `QuestItemHolder`，所以它会使用子项模板和 `QuestItemRender`。

第二个作用是识别“这一行是不是组头”。`GroupAdapter.IsGroupIndex(index)` 会比较显示数据的 `TemplateName` 是否等于 `groupViewName`。展开、收起、删除空组时都依赖这个判断。也就是说，`groupViewName` 不仅决定显示成哪个 Prefab，还决定框架把哪些行当作分组行处理。

一次完整刷新流程可以理解为：

1. 业务设置 `_quests.Data = CreateQuestRows()`。
2. `GroupAdapter` 扫描原始子项列表，发现 `Type = Main`、`Daily`、`Achievement`。
3. 每发现一个新的 `Type`，就创建一个组头行，并把它的 `TemplateName` 设置为 `groupViewName`。
4. 默认只显示组头行，因为新建组头的 `Expanded` 默认是 `false`。
5. 点击组头或调用 `_quests.Expand(index)` 后，框架把同 `Type` 的子项插入到这个组头后面。
6. `RecyclerView` 根据每一行的 `TemplateName` 创建对应模板：组头用 `QuestGroupHolder`，子项用 `QuestItemHolder`。

所以 `groupViewName` 必须满足这些条件：

| 要求 | 原因 |
| --- | --- |
| 不能为空 | 空字符串会导致 `GroupAdapter` 报错并停止刷新 |
| 必须是组头 `ViewHolder` 类型名 | `RecyclerView` 需要用它找到组头模板 |
| 组头模板必须在 `RecyclerView.Templates` 中 | 没有模板就无法创建组头实例 |
| 子项 `TemplateName` 不能等于 `groupViewName` | 否则子项会被误判成组头 |
| 需要注册组头对应的 `ItemRender` | 否则组头模板找不到渲染逻辑 |

常见错误示例：

```csharp
// 错误：这里传了子项模板名，所有组头都会被创建成子项样式。
_quests = UGListCreateHelper.CreateGroup<QuestRowData>(
    baseui.ScrollViewQuest,
    groupViewName: nameof(QuestItemHolder));

// 错误：子项 TemplateName 和组头模板名相同，子项会被 IsGroupIndex 误认为组头。
QuestRowData.Item(QuestGroupType.Main, 1001, "前往王城", 1, 1).TemplateName = nameof(QuestGroupHolder);
```

分组列表更新数据时，推荐替换整份数据或修改原始 `Data` 后调用 `NotifyDataChanged`，因为组头和子项的显示顺序需要重新计算。

```csharp
// 替换整份任务列表。
_quests.Data = CreateQuestRows();

// 在现有列表里增加任务。
_quests.Data.Add(QuestRowData.Item(QuestGroupType.Daily, 2003, "完成一次钓鱼", 0, 1));
_quests.Adapter.NotifyDataChanged();
```

## 数据更新

`UGList` 暴露 `Adapter`，可以直接增删改数据并触发刷新。

```csharp
_items.Adapter.Add(new BagItemData
{
    ItemId = 1003,
    Name = "Gem",
    Count = 3,
});

_items.Adapter.RemoveAt(0);
_items.Adapter.NotifyItemChanged(1);
_items.Adapter.NotifyDataChanged();
```

常用策略：

| 场景 | 推荐 API |
| --- | --- |
| 替换整个列表 | `_items.Data = newList` |
| 只更新可见项数据 | `_items.Adapter.NotifyItemChanged(index)` |
| 更新项尺寸或布局 | `_items.Adapter.NotifyItemChanged(index, relayout: true)` |
| 添加项 | `_items.Adapter.Add(data)` |
| 批量添加 | `_items.Adapter.AddRange(datas)` |
| 清空列表 | `_items.Adapter.Clear()` |

## 滚动和焦点

```csharp
_items.ScrollToStart(0);
_items.ScrollToCenter(20, smooth: true);
_items.ScrollTo(100, ScrollAlignment.Center, offset: 0f, smooth: true);

bool focused = _items.TryFocus(10, smooth: true);
_items.CommitFocusToChoice();
_items.ScrollToChoice(ScrollAlignment.Center, smooth: true);
```

索引含义：

| 属性 | 说明 |
| --- | --- |
| `FocusIndex` | 当前 EventSystem 导航焦点所在的数据索引 |
| `CurrentIndex` | RecyclerView 内部跟踪的滚动定位索引 |
| `ChoiceIndex` | 业务选择索引，点击或提交时变更 |

事件订阅：

```csharp
protected override void OnInitialize()
{
    _items.OnChoiceIndexChanged += OnChoiceChanged;
    _items.ScrollStopped += OnScrollStopped;
}

private void OnChoiceChanged(int index)
{
    Log.Info($"Choice changed: {index}");
}

private void OnScrollStopped()
{
    Log.Info("Scroll stopped.");
}
```

## ItemRender 生命周期

| 方法 | 调用时机 |
| --- | --- |
| `OnHolderAttached()` | 渲染器首次附加到某个 Holder 时 |
| `OnBind(TData, int)` | Holder 被绑定到新数据时 |
| `OnSelectionChanged(bool)` | 业务选择状态变化时 |
| `OnClear()` | 当前数据绑定被清理或复用前 |
| `OnHolderDetached()` | 渲染器从 Holder 分离时 |

建议：

1. `OnBind` 只做数据到 UI 的赋值。
2. 按钮监听、一次性缓存放在 `OnHolderAttached`。
3. 与当前数据相关的临时状态在 `OnClear` 里重置。
4. 需要异步加载图片时，记录 `CurrentBindingVersion`，回调时用 `IsBindingCurrent(version)` 判断 Holder 是否仍然绑定同一份数据。

## Inspector 常用配置

| 配置 | 说明 |
| --- | --- |
| `Direction` | 垂直、水平或自定义方向 |
| `Alignment` | 列表项在交叉轴上的对齐方式 |
| `Spacing` | 列表项间距 |
| `Padding` | 内容区域内边距 |
| `Scroll` | 一直可滚动、禁用滚动或仅内容溢出时滚动 |
| `Snap` | 停止滚动后吸附到最近项 |
| `ScrollbarVisibility` | 一直隐藏、一直显示或仅内容可滚动时显示 |
| `Templates` | 列表项模板数组 |

布局类型：

| 类型 | 说明 |
| --- | --- |
| `LinearLayoutManager` | 单列或单行列表 |
| `GridLayoutManager` | 网格列表 |
| `PageLayoutManager` | 分页列表 |
| `MixedLayoutManager` | 混合尺寸或混合模板列表 |
| `CircleLayoutManager` | 圆形布局 |

## API 速查

| API | 说明 |
| --- | --- |
| `UGListCreateHelper.Create<T>(RecyclerView)` | 创建普通列表 |
| `UGListCreateHelper.CreateMixed<T>(RecyclerView)` | 创建混合模板列表 |
| `UGListCreateHelper.CreateLoop<T>(RecyclerView)` | 创建循环列表 |
| `UGListCreateHelper.CreateGroup<T>(RecyclerView, string)` | 创建分组列表 |
| `UGList.Data` | 替换数据源 |
| `UGList.Adapter.RegisterItemRender<T>()` | 注册渲染器 |
| `UGList.ChoiceIndex` | 获取或设置业务选择 |
| `UGList.TryFocus(int, bool, ScrollAlignment)` | 聚焦指定项 |
| `UGList.ScrollTo(int, ScrollAlignment, float, bool, float)` | 滚动到指定项 |
| `Adapter.NotifyItemChanged(int, bool)` | 局部刷新或重布局 |
| `ItemRender.OnBind(TData, int)` | 绑定数据 |
| `ItemRender.OnSelectionChanged(bool)` | 更新选中表现 |

## 注意事项

1. 模板对象会在运行时隐藏并作为对象池来源，不要把模板当作真实显示项直接操作。
2. 混合模板列表必须保证 `TemplateName` 与 `ViewHolder` 类型名匹配。
3. `ItemRender` 必须有无参构造函数，框架会通过反射创建实例。
4. `RecyclerView` 相关方法必须在 Unity 主线程调用。
5. 列表项尺寸会影响布局计算，动态改变尺寸后需要使用 `relayout: true` 刷新。
