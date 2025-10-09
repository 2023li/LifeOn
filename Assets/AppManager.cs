using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Moyo.Unity;
using UnityEngine.SceneManagement;
using Sirenix.OdinInspector;


public enum AppLanguage
{

}

public class AppManager : MonoSingleton<AppManager>
{


    protected override void Awake()
    {
     base.Awake();   
    }
    // Start is called before the first frame update
    void Start()
    {
        uiController = new LOUIController();
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    [Button]
    public void Test()
    {
        LoadScene("Start", true,"UIPanel_Main");
    }


    LOUIController uiController;

    public List<string> NeedPreloadGameObject;

    public string TargetSceneName { get; set; }
    [Button]
    public void LoadScene(string scenesName, bool transition = true,params string[] address)
    {
        if (NeedPreloadGameObject==null)
        {
            NeedPreloadGameObject = new List<string>();
        }
        NeedPreloadGameObject.Clear();

       
        TargetSceneName = scenesName;


        foreach (string s in address)
        {
            NeedPreloadGameObject.Add(s);
        }
       


        if (transition)
        {
            SceneManager.LoadScene(LOConstant.SceneName.Transition);
        }
        else
        {
            // 直跳场景：不进过渡页，就只能同步/独立处理预加载了（一般不建议）
            SceneManager.LoadScene(TargetSceneName);
        }
    }
}
