using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moyo.Unity;
using Sirenix.OdinInspector;
using UnityEngine;

public class Test : MonoBehaviour
{
    // Start is called before the first frame update


    private void Start()
    {
    }


    public SupplyDef def;
    [Button]
    public void TestXX()
    {

        //if (phase==TurnPhase.回合结束阶段)
        //{
        //    GameContext.Instance.TechTree.AddProgressToActiveResearch(1);
        //}


        GameContext.Instance.ResourceNetwork.HighlightCoverage(def);

      
    }




   



}
