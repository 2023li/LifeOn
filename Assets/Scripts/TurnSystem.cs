using System;
using System.Collections;
using System.Collections.Generic;
using Moyo.Unity;
using Sirenix.OdinInspector;
using UnityEngine;

public class TurnSystem : MonoSingleton<TurnSystem>
{

    protected override bool IsDontDestroyOnLoad => false;

    public event Action OnTrunEnd;
    public event Action OnTrunStart;

    [Button]
    public void NextTurun()
    {
        OnTrunEnd?.Invoke();
        OnTrunStart?.Invoke();
    }



}
