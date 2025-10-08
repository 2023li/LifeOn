using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.VisualScripting;
using UnityEngine;


[RequireComponent(typeof(Building))]
public abstract class BulidingAbility : MonoBehaviour
{
    protected Building building;

    public static T Add<T>(Building building) where T : BulidingAbility
    {
        
        var ability =  building.AddComponent<T>();
        ability.building = building;
        ability.OnAdd();
        return ability;
    }

    protected abstract void OnAdd();
   
    public abstract void Remove();
}
