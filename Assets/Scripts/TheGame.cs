using System.Collections;
using System.Collections.Generic;
using Moyo.Unity;
using UnityEngine;




public class TheGame : MonoSingleton<TheGame>
{

    [SerializeField] private BuildingSelector buildingSelector;
    public BuildingSelector BuildingSelector { get { return buildingSelector; } }









    protected override bool IsDontDestroyOnLoad => false;
    IBackRegister.GameBackHandle gameBackHandle;
    protected override void Awake()
    {
        base.Awake ();
        GameContext.Instance.Init();

        if (buildingSelector == null)
        {
            buildingSelector = GetComponent<BuildingSelector>();
        }

        gameBackHandle = new();

    }
    private void OnEnable()
    {
        if (InputManager.HasInstance)
        {
            InputManager.Instance.Register(gameBackHandle);
        }
    }

    public void Start()
    {
       _ = UIManager.Instance.ShowPanel<UIPanel_GameMain>(UIManager.UILayer.Main);
        AppEventArgs.Tiggle(AppEventEnum.开始游戏);
        Debug.Log("游戏开始");

    }
    private void OnDisable()
    {
        if (InputManager.HasInstance)
        {
            InputManager.Instance.UnRegister(gameBackHandle);
        }
    }


    public void Pause()
    {
        _ = UIManager.Instance.ShowPanel<UIPanel_Pause>(UIManager.UILayer.Main);
    }

}
