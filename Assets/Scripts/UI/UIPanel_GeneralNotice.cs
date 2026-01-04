using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moyo.Unity;
using Unity.VisualScripting;
using UnityEngine;



public class UIPanel_GeneralNotice : PanelBase
{

    public override UILayer Layer => UILayer.Notice;

    [SerializeField] private GameObject waitIcon;
    [SerializeField] private GameObject autoSave;
    [SerializeField] private GameObject load;


    private void Clear()
    {
        waitIcon.SetActive(false);
        autoSave.SetActive(false);
        load.SetActive(false);
    }

    public static async Task ShowWait()
    {
        var ins =  await UIManager.Instance.ShowPanel<UIPanel_GeneralNotice>();
        ins.Clear();
        ins.waitIcon.SetActive(true);
    }
    public static void HideWait()
    {
        UIManager.Instance.HidePanel<UIPanel_GeneralNotice>();
    }
   

    public static void TriggerSystemNotice()
    {

    }
    public static void BottomBanner()
    {

    }
}
