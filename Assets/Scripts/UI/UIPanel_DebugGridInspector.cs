using Moyo.Unity;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

public class UIPanel_DebugGridInspector : PanelBase
{
    private const string DefaultCoordinateText = "坐标: -";

    [LabelText("坐标文本")]
    [SerializeField] private TMP_Text _coordinateText;

    protected override void Awake()
    {
        base.Awake();
        SetCoordinateText(null);
    }

    private void OnEnable()
    {
        if (InputManager.HasInstance)
        {
            InputManager.Instance.OnMouseMove += HandleMouseMove;
            HandleMouseMove(InputManager.Instance.MousePos);
        }
        else
        {
            SetCoordinateText(null);
        }
    }

    private void OnDisable()
    {
        if (InputManager.HasInstance)
        {
            InputManager.Instance.OnMouseMove -= HandleMouseMove;
        }
    }

    private void HandleMouseMove(Vector2 screenPoint)
    {
        RefreshCoordinate(screenPoint);
    }

    private void RefreshCoordinate(Vector2 screenPoint)
    {
        if (_coordinateText == null)
        {
            return;
        }

        if (!InputManager.HasInstance || !GridSystem.HasInstance)
        {
            SetCoordinateText(null);
            return;
        }

        var gridSystem = GridSystem.Instance;
        var inputManager = InputManager.Instance;

        if (gridSystem == null || inputManager == null || gridSystem.mapGrid == null || inputManager.RealCamera == null)
        {
            SetCoordinateText(null);
            return;
        }

        var coordinate = gridSystem.GetScreenPointCoordinates(screenPoint);
        SetCoordinateText(coordinate);
    }

    private void SetCoordinateText(Vector3Int? coordinate)
    {
        if (_coordinateText == null)
        {
            return;
        }

        if (!coordinate.HasValue)
        {
            _coordinateText.text = DefaultCoordinateText;
            return;
        }

        Vector3Int value = coordinate.Value;
        _coordinateText.text = $"坐标: ({value.x}, {value.y}, {value.z})";
    }
}
