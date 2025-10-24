using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;



[Serializable]
public class TechNode
{

    [LabelText("节点ID")] public string Id;
    [LabelText("显示名称")] public string DisplayName;
    [LabelText("描述")][TextArea] public string Description;
    [LabelText("图标")] public Sprite Icon;
    [LabelText("编辑器位置")] public Vector2 EditorPosition;
    [LabelText("前置节点")] public List<string> Prerequisites = new List<string>();
    [LabelText("解锁条件")][SerializeReference] public List<Condition> UnlockConditions = new List<Condition>();
    [LabelText("解锁效果")][SerializeReference] public List<Effect> OnUnlockEffects = new List<Effect>();

    
    public bool CanUnlock(IGameContext ctx, out string reason)
    {
        return ConditionUtility.TryEvaluateConditions(UnlockConditions, null, ctx, out reason);
    }

    public void ApplyUnlockEffects(IGameContext ctx)
    {
        if (OnUnlockEffects == null)
        {
            return;
        }

        foreach (Effect effect in OnUnlockEffects)
        {
            if (effect == null)
            {
                continue;
            }

            effect.Apply(null, ctx);
        }
    }
}




public class TechTree
{
    private readonly Dictionary<string, TechNode> _nodes = new Dictionary<string, TechNode>();
    public string StartingNodeId { get; private set; } = string.Empty;

    public void LoadFromAsset(TechTreeAsset asset)
    {
        _nodes.Clear();
        StartingNodeId = string.Empty;

        if (asset == null)
        {
            return;
        }

        StartingNodeId = asset.StartingNodeId;
        if (asset.Nodes == null)
        {
            return;
        }

        foreach (TechNode node in asset.Nodes)
        {
            if (node == null || string.IsNullOrWhiteSpace(node.Id))
            {
                continue;
            }

            _nodes[node.Id] = node;
        }
    }

    public bool HasNode(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return false;
        }

        return _nodes.ContainsKey(id);
    }

     public bool TryGetNode(string id, out TechNode node)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            node = null;
            return false;
        }

        return _nodes.TryGetValue(id, out node);
    }

    public IEnumerable<TechNode> GetAllNodes()
    {
        return _nodes.Values;
    }



    
    
}
