using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moyo.Unity;
using Sirenix.OdinInspector;
using UnityEngine;

public class Test : MonoBehaviour
{
    // Start is called before the first frame update



    [Button]
    public void TestXX()
    {
        GameContext.Instance.TechTree.AddProgressToActiveResearch(1);
    }



   
}
