using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moyo.Unity;
using UnityEngine;




public class TheGame : MonoSingleton<TheGame>,IBackHandler
{

    [SerializeField] private BuildingSelector buildingSelector;
    public BuildingSelector BuildingSelector { get { return buildingSelector; } }

    protected override bool IsDontDestroyOnLoad => false;

    public int BackPriority => BackPrioritySort.GameBack;

    protected override void Awake()
    {
        base.Awake ();

       

        if (buildingSelector == null)
        {
            buildingSelector = GetComponent<BuildingSelector>();
        }


    }
    private void OnEnable()
    {
        if (InputManager.HasInstance)
        {
            InputManager.Instance.Register(this);
        }
    }

    public void Start()
    {
      

    }
    private void OnDisable()
    {
        if (InputManager.HasInstance)
        {
            InputManager.Instance.UnRegister(this);
        }
    }
    public async Task InitGame()
    {

        GameContext.Instance.Clear();
        await SupplyLib.Init();
        await UIManager.Instance.ShowPanel<UIPanel_GameMain>();
        AppEventArgs.Tiggle(AppEventEnum.开始游戏);
        Debug.Log("游戏开始");

    }

    public void Pause()
    {
        _ = UIManager.Instance.ShowPanel<UIPanel_Pause>();
    }

    public bool TryHandleBack()
    {
        Pause();
        return true;
    }
}
