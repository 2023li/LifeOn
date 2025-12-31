using Moyo.Unity;
using Sirenix.OdinInspector;
using UnityEngine;

public class UIPanel_SaveSelection : PanelBase
{
    [SerializeField,LabelText("存档父对象")]
    private RectTransform rt_saveParents;

    [SerializeField,LabelText("存档Item预制体")]
    private UIItem_SaveItem prefab_SaveItem;


    public override void Show(params object[] args)
    {
        base.Show(args);

        var list = PersistentManager.Instance.GetAllSaves();

        foreach (var item in list)
        {



        }


    }

}
