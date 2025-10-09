using System.Collections;
using System.Collections.Generic;
using Moyo.Unity;
using UnityEngine;




public class TheGame : MonoSingleton<TheGame>
{
    protected override bool IsDontDestroyOnLoad => true;


    public void Start()
    {
       _ = UIManager.Instance.ShowPanel<UIPanel_GameMain>(UIManager.UILayer.Main);
    }

    public void GetAllBuildingClassify()
    {

    }



}
