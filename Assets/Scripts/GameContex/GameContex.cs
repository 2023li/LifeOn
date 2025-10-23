// Assets/Game/Scripts/Runtime/Services.cs
using System;
using System.Collections.Generic;
using Moyo.Unity;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine;

public interface IGameContext
{
    ResourceNetwork ResourceNetwork { get; }
    TechTree TechTree { get; }

    CityEnvironment Environment { get; }
}

public class GameContext : Singleton<GameContext>, IGameContext
{

    protected  GameContext() { }

    private ResourceNetwork resourceNetwork = new ResourceNetwork();
    private TechTree techTree = new TechTree();
    private CityEnvironment environment = new CityEnvironment();

    /// <summary>资源网络：负责仓库注册、库存查询。</summary>
    public ResourceNetwork ResourceNetwork => resourceNetwork;

    /// <summary>科技树：用于校验科技节点。</summary>
    public TechTree TechTree => techTree;

    /// <summary>城市环境：用于处理治安、医疗、美化等光环。</summary>
    public CityEnvironment Environment => environment;

    public void Init()
    {
       
        // 兜底初始化，避免在场景中缺失引用。
        if (resourceNetwork == null)
        {
            resourceNetwork = new ResourceNetwork();
        }

        if (techTree == null)
        {
            techTree = new TechTree();
        }

        if (environment == null)
        {
            environment = new CityEnvironment();
        }
    }
}






