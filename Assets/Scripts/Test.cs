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

    private bool EnableGamePlayMap = false;
    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            if (!EnableGamePlayMap)
            {
                InputManager.Instance.EnableGamePlayMap();
                EnableGamePlayMap = true;
            }
            else
            {
                InputManager.Instance.DisableGamePlayMap();
                EnableGamePlayMap = false;
            }

        }

    }

    public List<Vector3Int> t;
    [Button]
    public void TestXX()
    {
      GridSystem.Instance.SetHighlight ( CoordinateCalculator.GetReachableCellsByMovePower(t, 5));
       

      
    }




   



}
