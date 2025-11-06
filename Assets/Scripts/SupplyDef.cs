

using System;
using Sirenix.OdinInspector;
using UnityEngine;


public enum SupplyCategory
{
    独特,
    一级食物,
    二级食物,
}





[CreateAssetMenu(fileName = "SupplyDef_", menuName = "Game/SupplyDef")]
public class SupplyDef : ScriptableObject
{
    [LabelText("物资ID")]
    public string Id;

    [LabelText("名称")]
    public string DisplayName; // "食物"

    [LabelText("本质")]
    public SupplyCategory Category;

    [LabelText("图标")]
    public Sprite Icon;

    
    public DisplayOption DisplaySetting = DisplayOption.常规;

    [LabelText("占用单位")]
    public int OccupationUnit = 1;

    [LabelText("基础运输半径")]
    public int BaseTransportationRadius = 5;


    [LabelText("损耗率")]
    [Tooltip("一般来说控制在5%以内，实际物资损耗还会受到仓库影响,最小单位为0.005f")]
    [Range(0,0.05f)]
    [OnValueChanged(nameof(OnBaseLossRateChanged))]
    public float BaseLossRate;

    private void OnBaseLossRateChanged()
    {
        BaseLossRate = Mathf.Round(BaseLossRate / 0.005f) * 0.005f;
    }




    public enum DisplayOption
    {
        常规,
        不显示,
        宝藏,
        
    }
}

[Serializable]
public struct SupplyAmount
{
    public SupplyDef Resource;
    public int Amount;
}
