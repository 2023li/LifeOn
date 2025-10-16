

using System;
using UnityEngine;


[CreateAssetMenu(fileName = "SupplyDef_", menuName = "Game/SupplyDef")]
public class SupplyDef : ScriptableObject
{
    public string Id;          // "food", "clothes" ...
    public string DisplayName; // "食物"
    public Sprite Icon;
}

[Serializable]
public struct SupplyAmount
{
    public SupplyDef Resource;
    public int Amount;
}
