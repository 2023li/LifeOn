using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class QC
{

    static bool 打印需要优化的方法 = false;

    

    public static void QC_待优化标记(string m)
    {

        if (打印需要优化的方法)
        {
            Debug.LogWarning(m);
        }
    } 



}
