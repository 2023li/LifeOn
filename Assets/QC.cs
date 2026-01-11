using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class QC
{

    static bool 打印需要优化的方法 = false;

    

    public static void 待优化(string m = null)
    {

        if (打印需要优化的方法)
        {
            Debug.LogWarning($"待优化：{m}");
        }
    } 

    public static void 未实现(string m =null)
    {
        Debug.LogError($"功能未实现: {m}");
    }

}
