using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;



public class TechTreeManager
{
    private readonly Dictionary<string, TechNodeData> _nodes = new Dictionary<string, TechNodeData>();
    public string StartingNodeId { get; private set; } = string.Empty;


    public bool HasNode(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return false;
        }

        return _nodes.ContainsKey(id);
    }

     public bool TryGetNode(string id, out TechNodeData node)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            node = null;
            return false;
        }

        return _nodes.TryGetValue(id, out node);
    }

    public IEnumerable<TechNodeData> GetAllNodes()
    {
        return _nodes.Values;
    }



    
    
}
