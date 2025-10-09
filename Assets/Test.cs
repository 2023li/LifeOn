using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moyo.Unity;
using UnityEngine;

public class Test : MonoBehaviour
{
    // Start is called before the first frame update
    private async void Start()
  {
    Debug.Log("START");
      _  = await UIManager.Instance.ShowPanel<UIPanel_Main>(UIManager.UILayer.Main);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
