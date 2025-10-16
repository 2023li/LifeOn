

using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;


[CreateAssetMenu(fileName = "Resource", menuName = "Game/Resource")]
public class ResourceDef : ScriptableObject
{
    public string Id;          // "food", "clothes" ...
    public string DisplayName; // "食物"
    public Sprite Icon;
}

[Serializable]
public struct ResourceAmount
{
    public ResourceDef Resource;
    public int Amount;
}
