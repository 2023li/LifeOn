using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class SupplyLib : ScriptableObject
{
    public Dictionary<string, SupplyDef> dic_ID_SupplyDef;


    public static SupplyLib Ins;


    [SerializeField] public List<SupplyDef> allSupply;

    public static async Task Init()
    {
        if (Ins != null)
        {
            Debug.LogWarning("SupplyLib 已经初始化，跳过重复加载。");
            return;
        }

        Ins = await AssetsManager.Instance.LoadAssetAsync<SupplyLib>(LOConstant.AssetsKey.Address_SupplyLib);
        Ins.dic_ID_SupplyDef = new Dictionary<string,SupplyDef>();

        foreach (var item in Ins.allSupply)
        {
            if (string.IsNullOrEmpty(item.Id))
            {
                Debug.LogWarning("存在 SupplyDef 的 Id 为空！");
                continue;
            }

            if(!Ins.dic_ID_SupplyDef.TryAdd(item.Id, item))
            {
                Debug.LogWarning($"重复的 SupplyDef Id：{item.Id}");
            }
        }
    }


    public SupplyDef GetSupplyDef(string id)
    {
        if (dic_ID_SupplyDef == null)
        {
            Debug.LogWarning("SupplyLib未执行Init");
            return null;
        }

        if (dic_ID_SupplyDef.ContainsKey(id))
        {
            return dic_ID_SupplyDef[id];
        }
        Debug.LogWarning($"不存在id为 {id} 的SupplyDef");
        return null;
    }

}
