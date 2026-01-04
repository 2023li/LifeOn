using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Moyo.Unity;
using Sirenix.OdinInspector;
using System.Threading.Tasks;

/*
 * 这个脚本之后需要做启动屏幕效果
 */

public class BootScenes : MonoBehaviour
{


    private bool isLoaded = false;
    public TMP_Text t;
    
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("do some");


    }

    float time = 2f;

    // Update is called once per frame
    void Update()
    {
        // 如果已经触发加载，就不再执行倒计时逻辑
        if (isLoaded) return;

        time -= Time.deltaTime;
        t.text = time.ToString("F1"); // 建议保留一位小数

        if (time < 0)
        {
            isLoaded = true; // 锁定，防止下一帧再次调用
            AppManager.Instance.LoadStartScene();
        }


    }
    [Button]
    async Task Test()
    {
        await UIPanel_UniversalSelectionBox.ShowBox("测试", "这是一个测试");
    }

}
