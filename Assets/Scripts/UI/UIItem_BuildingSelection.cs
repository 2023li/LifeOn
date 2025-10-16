using System;
using UnityEngine;
using UnityEngine.UI;
using Lean.Pool; // LeanPool
using Moyo.Unity;

public enum BuildingClassify
{
    基础,
    市政,
    工业类,
    农业类,
}

public class UIItem_BuildingSelection : MonoBehaviour
{
    [AutoBind] public GameObject panel_选择建筑类型;
    [AutoBind] public GameObject panel_选择建筑;

    [Header("UI 引用")]
    [AutoBind] public RectTransform Content;                 // 分类按钮容器
    [AutoBind] public RectTransform BuildBuildingBtnContent; // 建筑按钮容器

    [AutoBind] public Button btn_Hide;

    // ✅ 新增：返回到分类列表的按钮（请把它放在 “panel_选择建筑” 面板里）
    [AutoBind] public Button btn_BackToClass;

    [Header("预制体")]
    public IconTextButton btnPrefabs;

    private void Reset()
    {
        this.AutoBindFields();
    }

    private void Awake()
    {
        if (btn_Hide != null)
        {
            btn_Hide.onClick.RemoveAllListeners();
            btn_Hide.onClick.AddListener(Hide);
        }

        // ✅ 绑定返回按钮
        if (btn_BackToClass != null)
        {
            btn_BackToClass.onClick.RemoveAllListeners();
            btn_BackToClass.onClick.AddListener(BackToClassList);
        }
    }

    /// <summary>
    /// 展示分类按钮：为每个 BuildingClassify 枚举项生成一个按钮
    /// </summary>
    public void Show()
    {
        if (!ValidateRefs()) return;

        // 显示“选择建筑类型”，隐藏“选择建筑”
        if (panel_选择建筑类型) panel_选择建筑类型.SetActive(true);
        if (panel_选择建筑) panel_选择建筑.SetActive(false);

        // 清空旧分类 & 旧建筑按钮
        ClearBuildingClassButtons();
        ClearBuildingButtons();

        // 生成分类按钮
        foreach (BuildingClassify classify in Enum.GetValues(typeof(BuildingClassify)))
        {
            CreateBuildingClassButton(classify);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(Content);
        gameObject.SetActive(true);
    }

    private void CreateBuildingClassButton(BuildingClassify classify)
    {
        IconTextButton item = LeanPool.Spawn(btnPrefabs, Content);

        var rt = (RectTransform)item.transform;
        rt.localScale = Vector3.one;
        rt.anchoredPosition3D = Vector3.zero;
        item.gameObject.name = $"Btn_Class_{classify}";

        item.SetContent(classify.ToString(), null);

        var captured = classify;
        item.SetOnClick(() => ShowBuilingClasss(captured));
    }

    private void CreateBuildingButton(BuildingArchetype buildingDef)
    {
        IconTextButton item = LeanPool.Spawn(btnPrefabs, BuildBuildingBtnContent);

        var rt = (RectTransform)item.transform;
        rt.localScale = Vector3.one;
        rt.anchoredPosition3D = Vector3.zero;
        item.gameObject.name = $"Btn_Build_{buildingDef.BuildingID}";

        item.SetContent(buildingDef.BuildingName, null);

        var captured = buildingDef;
        item.SetOnClick(() => BuildBuilding(captured));
    }

    private void ClearBuildingClassButtons()
    {
        if (Content == null) return;
        for (int i = Content.childCount - 1; i >= 0; i--)
        {
            var child = Content.GetChild(i);
            if (child != null) LeanPool.Despawn(child);
        }
    }

    private void ClearBuildingButtons()
    {
        if (BuildBuildingBtnContent == null) return;
        for (int i = BuildBuildingBtnContent.childCount - 1; i >= 0; i--)
        {
            var child = BuildBuildingBtnContent.GetChild(i);
            if (child != null) LeanPool.Despawn(child);
        }
    }

    /// <summary>
    /// 点击分类按钮 → 在建筑容器中显示该分类的所有建筑，并切换到“选择建筑”面板
    /// </summary>
    public void ShowBuilingClasss(BuildingClassify classify)
    {
        if (ResourceRouting.Instance == null)
        {
            Debug.LogError("[UIItem_BuildingSelection] ResourceRouting.Instance 为空。");
            return;
        }

        var allBuilding = ResourceRouting.Instance.GetClassAllBuildingDef(classify);
        if (allBuilding == null)
        {
            Debug.LogWarning($"[UIItem_BuildingSelection] 未找到分类 {classify} 的建筑定义。");
            return;
        }

        ClearBuildingButtons();

        foreach (var def in allBuilding)
        {
            if (def == null) continue;
            CreateBuildingButton(def); // ✅ 修正：传入 def
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(BuildBuildingBtnContent);

        if (panel_选择建筑类型) panel_选择建筑类型.SetActive(false);
        if (panel_选择建筑) panel_选择建筑.SetActive(true);

        Debug.Log($"[UIItem_BuildingSelection] 分类 {classify} 的建筑列表已生成。");
    }

    public void BuildBuilding(BuildingArchetype def)
    {       
        // TODO: 你的建造逻辑

        BuildingBuilder.BuildingEvent.Trigger(def);
    
    }

    /// <summary>
    /// ✅ 返回到分类列表
    /// </summary>
    public void BackToClassList()
    {
        // 仅清空“建筑”按钮，保留已生成的分类按钮
        ClearBuildingButtons();

        if (panel_选择建筑类型) panel_选择建筑类型.SetActive(true);
        if (panel_选择建筑) panel_选择建筑.SetActive(false);

        LayoutRebuilder.ForceRebuildLayoutImmediate(Content);

        Debug.Log("[UIItem_BuildingSelection] 已返回到分类列表。");
    }

    public void Hide()
    {
        ClearBuildingClassButtons();
        ClearBuildingButtons();

        if (panel_选择建筑类型) panel_选择建筑类型.SetActive(false);
        if (panel_选择建筑) panel_选择建筑.SetActive(false);

        gameObject.SetActive(false);
    }

    private bool ValidateRefs()
    {
        bool ok = true;
        if (panel_选择建筑类型 == null) { Debug.LogWarning("[UIItem_BuildingSelection] panel_选择建筑类型 未绑定。"); ok = false; }
        if (panel_选择建筑 == null) { Debug.LogWarning("[UIItem_BuildingSelection] panel_选择建筑 未绑定。"); ok = false; }
        if (Content == null) { Debug.LogWarning("[UIItem_BuildingSelection] Content（分类容器）未绑定。"); ok = false; }
        if (BuildBuildingBtnContent == null) { Debug.LogWarning("[UIItem_BuildingSelection] BuildBuildingBtnContent（建筑容器）未绑定。"); ok = false; }
        if (btnPrefabs == null) { Debug.LogWarning("[UIItem_BuildingSelection] btnPrefabs 未赋值。"); ok = false; }
        // btn_BackToClass 可选，但更推荐绑定
        if (btn_BackToClass == null) { Debug.LogWarning("[UIItem_BuildingSelection] 提示：未绑定 btn_BackToClass（返回按钮）。"); }
        return ok;
    }
}
