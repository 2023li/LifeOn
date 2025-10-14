using System;
using System.Collections;
using System.Collections.Generic;
using Moyo.Unity;
using UnityEngine;
using UnityEngine.UI;

public class UIItem_HaloPanel : MonoBehaviour
{
    [AutoBind] public Toggle toggle_环境;
    [AutoBind] public Toggle toggle_治安;
    [AutoBind] public Toggle toggle_医疗;


    private List<Func<Vector3Int, bool>> currentConditions = new List<Func<Vector3Int, bool>>(15);

    private void Reset()
    {
        this.AutoBindFields();
    }

    private void Awake()
    {


        toggle_环境.onValueChanged.AddListener((b) =>
        {

        });
    }


    private void Refresh()
    {

    }


}
