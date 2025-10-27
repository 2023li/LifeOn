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
        TurnSystem.OnTurnPhaseChange += TestXX;
    }


    [Button]
    public void TestXX(TurnPhase phase)
    {

        if (phase==TurnPhase.回合结束阶段)
        {
            GameContext.Instance.TechTree.AddProgressToActiveResearch(1);
        }
      
    }



   
}
