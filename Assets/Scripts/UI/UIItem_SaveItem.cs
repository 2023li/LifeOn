using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIItem_SaveItem : MonoBehaviour
{
    private string currentSaveID;

    [SerializeField] private TMP_Text text_GameName;
    [SerializeField] private TMP_Text text_游戏阶段;
    [SerializeField] private TMP_Text text_人口数;
    [SerializeField] private TMP_Text text_保存时间;
    [SerializeField] private Button btn_LoadThis;
    [SerializeField] private Button btn_DeleteThis;

    public void Init(GameSaveData data)
    {
        currentSaveID = data.saveid;

        // 设置文本，增加判空防止报错
        if (text_GameName) text_GameName.text = data.saveName;
        if (text_保存时间) text_保存时间.text = data.lastSaveDate;

        // 如果数据里有对应字段，也可以在这里赋值
        // if (text_游戏阶段) text_游戏阶段.text = ...
        // if (text_人口数) text_人口数.text = ...
    }
    private void Awake()
    {
        if (btn_DeleteThis)
        {
            btn_DeleteThis.onClick.AddListener(() =>
            {
                if (!string.IsNullOrEmpty(currentSaveID))
                {
                    PersistentManager.Instance.DeleteGame(currentSaveID);
                }
            });
        }
    }
    private void OnEnable()
    {
        btn_LoadThis.onClick.AddListener(OnLoadClick);
    }

    private void OnDisable()
    {
        btn_LoadThis.onClick.RemoveListener(OnLoadClick);
    }

    private void OnLoadClick()
    {
        if (!string.IsNullOrEmpty(currentSaveID))
        {
            Debug.Log($"[UIItem_SaveItem] 请求加载存档: {currentSaveID}");
            // 假设 PersistentManager 有 LoadGame 方法
            PersistentManager.Instance.LoadGame(currentSaveID);

            // 如果加载需要关闭当前界面，可以在这里调用，或者由 LoadGame 内部触发事件
            // UIManager.Instance.BackTopPanel(); 
        }
    }
    
}
