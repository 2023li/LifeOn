using System.Collections;
using System.Collections.Generic;
using Moyo.Unity;
using UnityEngine;




public class TheGame : MonoSingleton<TheGame>
{

    private BuildingSelector buildingSelector;
    public BuildingSelector BuildingSelector { get { return buildingSelector; } }









    protected override bool IsDontDestroyOnLoad => false;

    protected override void Awake()
    {
        base.Awake ();
        GameContext.Instance.Init();


        buildingSelector = GetComponent<BuildingSelector>();


    }
    public void Start()
    {
       _ = UIManager.Instance.ShowPanel<UIPanel_GameMain>(UIManager.UILayer.Main);

        _ = UIManager.Instance.ShowPanel<UIPanel_DebugGridInspector>(UIManager.UILayer.DebugInfo);


        LOAppEvent.Tigger(LOAppEventType.开始游戏);


    }




}
