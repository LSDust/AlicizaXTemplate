using System.Collections.Generic;
using AlicizaX;
using AlicizaX.Resource.Runtime;
using AlicizaX.UI;
using AlicizaX.UI.Runtime;
using Cysharp.Threading.Tasks;
using Game.UI;
using GameLogic.UI;
using UnityEngine;
using UnityEngine.EventSystems;

public class TestData : IMixedViewData
{
    public string Name;
    public string TemplateName { get; set; }
}

[UIUpdate]
[Window(UILayer.UI, false, 3)]
public class UILoadUpdate : UITabWindow<ui_UILoadUpdateWindow>
{
    private UGMixedList<TestData> _list;

    protected override void OnInitialize()
    {
        _list = UGListCreateHelper.CreateMixed<TestData>(baseui.ScrollViewTestList);
        _list.RegisterItemRender<ImageScrollItemRender>();
        _list.RegisterItemRender<TextScrollItemRender>();
        baseui.ImgBackGround.color = Color.gray;
        baseui.BtnTest.onClick.AddListener(OnTestClick);
        SetListAndFocusFirst(CreateTestDataList(200));

        baseui.BtnQTest.onClick.AddListener(OnBtnQTestClick);
        baseui.BtnEscTest.onClick.AddListener(OnBtnEscTestClick);
        baseui.BtnETest.onClick.AddListener(OnBtnETestClick);
    }

    private void OnBtnETestClick()
    {
        _list.ScrollTo(100,ScrollAlignment.Center,0,true);
    }

    private void OnBtnEscTestClick()
    {
        Log.Info("Btn Esc Click");
        CloseSelf();
    }

    private void OnBtnQTestClick()
    {
        int index = Random.Range(1, 199);
        Debug.Log(index);
        _list.ScrollTo(index,ScrollAlignment.Center,0,true);
    }

    private static List<TestData> CreateTestDataList(int count)
    {
        List<TestData> testDataList = new List<TestData>(count);
        for (int i = 0; i < count; i++)
        {
            var prefabName = i % 2 == 0 ? nameof(TextViewHolder) : nameof(ImageViewHolder);
            testDataList.Add(new TestData() { Name = $"TestProp:{i}", TemplateName = prefabName });
        }

        return testDataList;
    }

    private void SetListAndFocusFirst(List<TestData> dataList)
    {
        _list.Data = dataList;
        FocusFirstItemAsync().Forget();
    }

    private async UniTaskVoid FocusFirstItemAsync()
    {
        await UniTask.Delay(5000);
        // Debug.Log("Focus");
        // _list.TryFocusEntry(MoveDirection.Down,true);
    }


    private void OnTestClick()
    {
        GameApp.UI.ShowUISync<UILogicTestAlert>();
    }
}
