using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Moyo.Unity;
using UnityEngine.UI;
using Sirenix.OdinInspector;
using FMODUnity;
using Unity.VisualScripting;
using System.Threading.Tasks;
using UnityEditor.Localization.Editor;
using UnityEngine.Localization;

public class UIPanel_Setting : PanelBase
{

    public override UILayer Layer => UILayer.Panel;

    [SerializeField, FoldoutGroup("导航栏")] private Toggle toggle_声音;
    [SerializeField, FoldoutGroup("导航栏")] private Toggle toggle_控制;
    [SerializeField, FoldoutGroup("导航栏")] private Toggle toggle_游戏;
    [SerializeField, FoldoutGroup("导航栏")] private Toggle toggle_图像;
    [SerializeField, FoldoutGroup("导航栏")] private Toggle toggle_辅助功能;

    [SerializeField, FoldoutGroup("导航栏")] private GameObject go_声音;
    [SerializeField, FoldoutGroup("导航栏")] private GameObject go_控制;
    [SerializeField, FoldoutGroup("导航栏")] private GameObject go_游戏;
    [SerializeField, FoldoutGroup("导航栏")] private GameObject go_图像;
    [SerializeField, FoldoutGroup("导航栏")] private GameObject go_辅助功能;

    [FoldoutGroup("本地化文本"),SerializeField]
    private LocalizedString localStr_确定保存标题;
    [FoldoutGroup("本地化文本"),SerializeField]
    private LocalizedString localStr_确定保存描述;
  
    protected void Awake()
    {
        BindToggleEvents();

        RefreshPanelState();

    }

    private void OnEnable()
    {
        
    }
    private void Start()
    {
        
    }

    private void OnDestroy()
    {
       
    }
    public override void Hide(params object[] args)
    {
        base.Hide(args);

    }
    public override bool Back(params object[] args)
    {
        if (!isSave)
        {
            string label = localStr_确定保存标题.GetLocalizedString();
            string des = localStr_确定保存描述.GetLocalizedString();
            _ = UIPanel_UniversalSelectionBox.ShowBox(label, des,
                () =>
                {
                    PersistentManager.Instance.SaveAppData();
                    UIManager.Instance.HidePanel(this);
                },

                () =>
                {
                    UIManager.Instance.HidePanel(this);
                }
                );
          return true;
        }


        UIManager.Instance.HidePanel(this);
        return true;
    }

    private void BindToggleEvents()
    {
        // 使用 Lambda 表达式直接绑定：isOn 为 true 时显示物体，false 时隐藏物体
        toggle_声音.onValueChanged.AddListener((isOn) => { go_声音.SetActive(isOn);AudioManager.Instance.PlayOneShot(AudioEventReference.Instance.UI_切换);}); 
        toggle_控制.onValueChanged.AddListener((isOn) => { go_控制.SetActive(isOn); AudioManager.Instance.PlayOneShot(AudioEventReference.Instance.UI_切换); });
        toggle_游戏.onValueChanged.AddListener((isOn) => { go_游戏.SetActive(isOn); AudioManager.Instance.PlayOneShot(AudioEventReference.Instance.UI_切换); });
        toggle_图像.onValueChanged.AddListener((isOn) => { go_图像.SetActive(isOn); AudioManager.Instance.PlayOneShot(AudioEventReference.Instance.UI_切换); });
        toggle_辅助功能.onValueChanged.AddListener((isOn) => { go_辅助功能.SetActive(isOn); AudioManager.Instance.PlayOneShot(AudioEventReference.Instance.UI_切换); });
    }

    private void RefreshPanelState()
    {
        // 强制根据当前Toggle的勾选状态刷新一次显隐，防止Inspector里手动设置乱了
        go_声音.SetActive(toggle_声音.isOn);
        go_控制.SetActive(toggle_控制.isOn);
        go_游戏.SetActive(toggle_游戏.isOn);
        go_图像.SetActive(toggle_图像.isOn);
        go_辅助功能.SetActive(toggle_辅助功能.isOn);

    }


    private bool isSave = false;
 
    public void SetDataDirty()
    {
        isSave = false;
    }
}
