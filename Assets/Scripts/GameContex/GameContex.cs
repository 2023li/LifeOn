// Assets/Game/Scripts/Runtime/Services.cs
using System;
using System.Collections.Generic;
using Moyo.Unity;
using UnityEngine;
using UnityEngine.Rendering;
using static TechTreeManager;

public interface IGameContext
{
    ResourceNetwork ResourceNetwork { get; }
    TechTreeManager TechTree { get; }
    HumanResourcesNetwork HumanResourcesNetwork { get; }
    CityEnvironment Environment { get; }
    TurnSystem Turn { get; }

}

public class GameContext : Singleton<GameContext>, IGameContext
{

    protected GameContext() { }

    private ResourceNetwork resourceNetwork;
    private TechTreeManager techTree;
    private CityEnvironment environment;
    private HumanResourcesNetwork humanResourcesNetwork;
    private TurnSystem turnSystem;
    /// <summary>资源网络：负责仓库注册、库存查询。</summary>
    public ResourceNetwork ResourceNetwork => resourceNetwork;

    /// <summary>科技树：用于校验科技节点。</summary>
    public TechTreeManager TechTree => techTree;

    /// <summary>城市环境：用于处理治安、医疗、美化等光环。</summary>
    public CityEnvironment Environment => environment;

    public HumanResourcesNetwork HumanResourcesNetwork => humanResourcesNetwork;

    public TurnSystem Turn => turnSystem;

    public void Init()
    {
        resourceNetwork = new ResourceNetwork();
        techTree = new TechTreeManager();
        environment = new CityEnvironment();
        humanResourcesNetwork = new HumanResourcesNetwork();
        turnSystem = new TurnSystem();
        
        Debug.Log("GameContext初始化完成");
    }

}






