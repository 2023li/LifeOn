using System;
using System.Collections.Generic; // 明确引用 Generic
using Moyo.Unity;
using Sirenix.OdinInspector;
using UnityEngine;

public class UIPanel_SaveSelection : PanelBase
{

    public override UILayer Layer => UILayer.Panel;

    [SerializeField, LabelText("存档父对象")]
    private RectTransform rt_saveParents;

    [SerializeField, LabelText("存档Item预制体")]
    private UIItem_SaveItem prefab_SaveItem;

    // --- 新增：用于缓存已生成的 Item ---
    private List<UIItem_SaveItem> _spawnedItems = new List<UIItem_SaveItem>();

    private void OnEnable()
    {
        PersistentManager.Instance.OnDeleteGameSave+= RefreshSaveItem;
    }
    private void OnDisable()
    {
        if (PersistentManager.HasInstance)
        {
            PersistentManager.Instance.OnDeleteGameSave -= RefreshSaveItem;
        }
    }

    public override void Show(params object[] args)
    {
        base.Show(args);

        RefreshSaveItem();
    }

    private void RefreshSaveItem()
    {
        // 获取存档数据列表
        List<GameSaveData> saveList = PersistentManager.Instance.GetAllSaves();

        // 防止数据为空导致报错
        if (saveList == null) saveList = new List<GameSaveData>();

        // 1. 遍历数据进行显示或创建
        for (int i = 0; i < saveList.Count; i++)
        {
            UIItem_SaveItem item;

            // 如果缓存池里有足够的 Item，直接复用
            if (i < _spawnedItems.Count)
            {
                item = _spawnedItems[i];
                item.gameObject.SetActive(true);
            }
            else
            {
                // 缓存池不够，实例化新的并加入缓存
                item = Instantiate(prefab_SaveItem, rt_saveParents);
                _spawnedItems.Add(item);
            }

            // 初始化数据
            item.Init(saveList[i]);
        }

        // 2. 隐藏多余的 Item (例如上次显示了10个，这次只有5个，需要隐藏后5个)
        for (int i = saveList.Count; i < _spawnedItems.Count; i++)
        {
            _spawnedItems[i].gameObject.SetActive(false);
        }
    }

    public override void Hide(params object[] args)
    {
        base.Hide(args);
        // 如果需要在关闭界面时彻底清理（通常不需要，除非内存紧张），可以在这里 Destroy
    }

   
}
