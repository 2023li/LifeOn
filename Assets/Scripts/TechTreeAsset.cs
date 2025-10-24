using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(menuName = "LifeOn/Tech Tree Asset", fileName = "TechTreeAsset")]
public class TechTreeAsset : ScriptableObject
{
    [LabelText("起始节点ID")] public string StartingNodeId;
    [LabelText("科技节点")]
    public List<TechNode> Nodes = new List<TechNode>();
}
