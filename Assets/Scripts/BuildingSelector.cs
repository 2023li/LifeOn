using UnityEngine.Tilemaps;
using UnityEngine;
using System;

public class BuildingSelector : MonoBehaviour
{
    [SerializeField] private TileBase highlightTile;
    private BuildingInstance _current;

    public event Action<BuildingInstance> Event_SelectedBuilding;

    private void OnEnable()
    {
       
            InputManager.Instance.OnMousePrimaryClick += HandleClick;
       
            
        
        
    }
   


    private void OnDisable()
    {
        if (InputManager.HasInstance)
        {
            InputManager.Instance.OnMousePrimaryClick -= HandleClick;
        }
        ClearHighlight();
    }

    private void HandleClick(Vector2 screenPoint)
    {
        

        if (!GridSystem.HasInstance || (InputManager.Instance?.IsBuildingMap() ?? false))
        {
            return;
        }

        Vector3Int cell = GridSystem.Instance.GetScreenPointCoordinates(screenPoint);
        if (BuildingInstance.TryGetAtCell(cell, out var building) && building?.Occupy?.Length > 0)
        {
            _current = building;
            GridSystem.Instance.SetHighlight(new GridSystem.HighlightSpec(building.Occupy, highlightTile ?? TileLib.GetTile(GameTileEnum.Tile_黄色)));
        }
        else
        {
            _current = null;
            ClearHighlight();
        }

        Event_SelectedBuilding?.Invoke(_current);
    }

    private void ClearHighlight()
    {
        if (GridSystem.HasInstance)
        {
            GridSystem.Instance.ClearHighlight();
        }
    }
}
