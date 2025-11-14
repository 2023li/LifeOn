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

     


        LOAppEvent.Tigger(LOAppEventType.开始游戏);
        Debug.Log("游戏开始");

    }




}
