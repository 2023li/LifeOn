using System.Collections.Generic;
using System.Text;
using Moyo.Unity;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIPanel_DebugGridInspector : PanelBase
{
    private const string DefaultCoordinateText = "坐标: -";
    private const string DefaultBuildingText = "建筑: -";
    private const string DefaultStorageText = "仓库: -";
    private const string DefaultSupplyLabel = "选择物资";

    [LabelText("坐标文本")]
    [SerializeField] private TMP_Text _coordinateText;

    [LabelText("建筑名称文本")]
    [SerializeField] private TMP_Text _buildingNameText;

    [LabelText("仓库信息文本")]
    [SerializeField] private TMP_Text _storageInfoText;

    [LabelText("仓储交互容器")]
    [SerializeField] private GameObject _storageControlsRoot;

    [LabelText("物资下拉按钮")]
    [SerializeField] private Button _supplyDropdownButton;

    [LabelText("物资下拉文本")]
    [SerializeField] private TMP_Text _supplyDropdownLabel;

    [LabelText("物资下拉列表根")]
    [SerializeField] private GameObject _supplyDropdownListRoot;

    [LabelText("物资选项按钮预制体")]
    [SerializeField] private Button _supplyOptionButtonPrefab;

    [LabelText("数量输入框")]
    [SerializeField] private InputField _supplyAmountInput;

    [LabelText("添加按钮")]
    [SerializeField] private Button _addSupplyButton;

    [LabelText("物资列表")]
    public SupplyDef[] SupplyOptions;

    private readonly List<Button> _optionButtons = new();
    private Vector3Int? _currentCell;
    private BuildingInstance _currentBuilding;
    private SupplyDef _selectedSupply;
    private bool _dropdownVisible;

    protected override void Awake()
    {
        base.Awake();
        SetCoordinateText(null);
        SetBuildingInfo(null);
        InitializeSupplyControls();
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
            ClearSelection();
        }

        RefreshSupplyOptionsUI();
    }

    private void OnDisable()
    {
        if (InputManager.HasInstance)
        {
            InputManager.Instance.OnMouseMove -= HandleMouseMove;
        }

        SetDropdownListActive(false);
    }

    private void OnDestroy()
    {
        if (_supplyDropdownButton != null)
        {
            _supplyDropdownButton.onClick.RemoveListener(ToggleSupplyDropdown);
        }

        if (_addSupplyButton != null)
        {
            _addSupplyButton.onClick.RemoveListener(HandleAddSupplyClicked);
        }

        ClearSupplyOptionButtons();
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
            ClearSelection();
            return;
        }

        var gridSystem = GridSystem.Instance;
        var inputManager = InputManager.Instance;

        if (gridSystem == null || inputManager == null || gridSystem.mapGrid == null || inputManager.RealCamera == null)
        {
            SetCoordinateText(null);
            ClearSelection();
            return;
        }

        var coordinate = gridSystem.GetScreenPointCoordinates(screenPoint);
        SetCoordinateText(coordinate);

        _currentCell = coordinate;
        UpdateSelectionAtCell(coordinate);
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

    private void UpdateSelectionAtCell(Vector3Int cell)
    {
        if (BuildingInstance.TryGetAtCell(cell, out BuildingInstance inst))
        {
            if (_currentBuilding != inst)
            {
                SetBuildingInfo(inst);
            }
            else
            {
                RefreshStorageDisplay();
            }
        }
        else
        {
            SetBuildingInfo(null);
        }
    }

    private void SetBuildingInfo(BuildingInstance building)
    {
        _currentBuilding = building;

        if (_buildingNameText != null)
        {
            if (building == null)
            {
                _buildingNameText.text = DefaultBuildingText;
            }
            else
            {
                string name = !string.IsNullOrEmpty(building.DisplayName) ? building.DisplayName : building.name;
                _buildingNameText.text = $"建筑: {name}";
            }
        }

        RefreshStorageDisplay();
    }

    private void RefreshStorageDisplay()
    {
        if (_storageInfoText == null)
        {
            return;
        }

        if (_currentBuilding == null || _currentBuilding.Storage == null)
        {
            _storageInfoText.text = DefaultStorageText;
            SetStorageControlsVisible(false);
            return;
        }

        Inventory storage = _currentBuilding.Storage;
        StringBuilder builder = new StringBuilder();
        builder.AppendLine($"仓库容量: {storage.TotalQuantity}/{storage.Capacity}");

        bool hasContent = false;
        foreach (SupplyAmount item in storage.EnumerateContents())
        {
            hasContent = true;
            SupplyDef resource = item.Resource;
            string display = resource != null && !string.IsNullOrEmpty(resource.DisplayName)
                ? resource.DisplayName
                : resource != null ? resource.name : "未知物资";
            builder.AppendLine($"{display}: {item.Amount}");
        }

        if (!hasContent)
        {
            builder.AppendLine("无物资");
        }

        _storageInfoText.text = builder.ToString();
        SetStorageControlsVisible(true);
    }

    private void SetStorageControlsVisible(bool visible)
    {
        bool shouldShow = visible && _currentBuilding != null && _currentBuilding.Storage != null && HasSupplyOptions();

        if (_storageControlsRoot != null)
        {
            _storageControlsRoot.SetActive(shouldShow);
        }

        if (_addSupplyButton != null)
        {
            _addSupplyButton.interactable = shouldShow;
        }

        if (_supplyDropdownButton != null)
        {
            _supplyDropdownButton.interactable = shouldShow;
        }

        if (_supplyAmountInput != null)
        {
            _supplyAmountInput.interactable = shouldShow;
            if (!shouldShow)
            {
                _supplyAmountInput.text = string.Empty;
            }
        }

        if (!shouldShow)
        {
            SetDropdownListActive(false);
            _selectedSupply = null;
            UpdateSelectedSupplyLabel();
        }
        else
        {
            if (_optionButtons.Count == 0)
            {
                RefreshSupplyOptionsUI();
            }
            else if (_selectedSupply == null)
            {
                foreach (SupplyDef option in SupplyOptions)
                {
                    if (option == null)
                    {
                        continue;
                    }

                    _selectedSupply = option;
                    break;
                }

                UpdateSelectedSupplyLabel();
            }
            else
            {
                UpdateSelectedSupplyLabel();
            }
        }
    }

    private void HandleAddSupplyClicked()
    {
        if (_currentBuilding?.Storage == null || !HasSupplyOptions() || _selectedSupply == null)
        {
            return;
        }

        if (_supplyAmountInput == null || !int.TryParse(_supplyAmountInput.text, out int amount) || amount <= 0)
        {
            return;
        }

        _currentBuilding.Storage.Add(_selectedSupply, amount);
        _supplyAmountInput.text = string.Empty;
        RefreshStorageDisplay();
    }

    private bool HasSupplyOptions()
    {
        if (SupplyOptions == null || SupplyOptions.Length == 0)
        {
            return false;
        }

        foreach (SupplyDef option in SupplyOptions)
        {
            if (option != null)
            {
                return true;
            }
        }

        return false;
    }

    private void ClearSelection()
    {
        _currentCell = null;
        SetBuildingInfo(null);
        SetDropdownListActive(false);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            SetCoordinateText(null);
            SetBuildingInfo(null);
            _selectedSupply = null;
            UpdateSelectedSupplyLabel();
            SetDropdownListActive(false);
            if (_supplyOptionButtonPrefab != null)
            {
                _supplyOptionButtonPrefab.gameObject.SetActive(false);
            }
        }
    }
#endif

    private void InitializeSupplyControls()
    {
        if (_supplyDropdownLabel != null)
        {
            _supplyDropdownLabel.text = DefaultSupplyLabel;
        }

        if (_supplyDropdownListRoot != null)
        {
            _supplyDropdownListRoot.SetActive(false);
        }

        if (_supplyDropdownButton != null)
        {
            _supplyDropdownButton.onClick.AddListener(ToggleSupplyDropdown);
        }

        if (_addSupplyButton != null)
        {
            _addSupplyButton.onClick.AddListener(HandleAddSupplyClicked);
        }

        if (_supplyAmountInput != null)
        {
            _supplyAmountInput.text = string.Empty;
            _supplyAmountInput.contentType = InputField.ContentType.IntegerNumber;
        }

        if (_supplyOptionButtonPrefab != null)
        {
            _supplyOptionButtonPrefab.gameObject.SetActive(false);
        }

        RefreshSupplyOptionsUI();
    }

    private void RefreshSupplyOptionsUI()
    {
        if (!Application.isPlaying)
        {
            UpdateSelectedSupplyLabel();
            return;
        }

        ClearSupplyOptionButtons();

        if (_supplyDropdownListRoot == null || _supplyOptionButtonPrefab == null)
        {
            return;
        }

        if (!HasSupplyOptions())
        {
            _selectedSupply = null;
            UpdateSelectedSupplyLabel();
            return;
        }

        RectTransform listRect = _supplyDropdownListRoot.GetComponent<RectTransform>();
        RectTransform templateRect = _supplyOptionButtonPrefab.GetComponent<RectTransform>();
        float elementHeight = templateRect != null ? templateRect.sizeDelta.y : 40f;
        float spacing = 4f;
        float offset = 0f;
        SupplyDef firstAvailable = null;

        foreach (SupplyDef option in SupplyOptions)
        {
            if (option == null)
            {
                continue;
            }

            Button optionButton = Instantiate(_supplyOptionButtonPrefab, listRect);
            optionButton.gameObject.SetActive(true);

            RectTransform optionRect = optionButton.GetComponent<RectTransform>();
            if (optionRect != null)
            {
                optionRect.anchorMin = new Vector2(0f, 1f);
                optionRect.anchorMax = new Vector2(0f, 1f);
                optionRect.pivot = new Vector2(0f, 1f);
                optionRect.anchoredPosition = new Vector2(0f, -offset);
                if (templateRect != null)
                {
                    optionRect.sizeDelta = templateRect.sizeDelta;
                }
            }

            TMP_Text label = optionButton.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                label.text = GetSupplyDisplayName(option);
            }

            SupplyDef captured = option;
            optionButton.onClick.AddListener(() => OnSupplyOptionSelected(captured));
            _optionButtons.Add(optionButton);

            offset += elementHeight + spacing;

            if (firstAvailable == null)
            {
                firstAvailable = option;
            }
        }

        if (listRect != null)
        {
            Vector2 size = listRect.sizeDelta;
            size.y = offset;
            listRect.sizeDelta = size;
        }

        if (firstAvailable != null)
        {
            if (_selectedSupply != firstAvailable)
            {
                OnSupplyOptionSelected(firstAvailable);
            }
            else
            {
                UpdateSelectedSupplyLabel();
            }
        }
        else
        {
            _selectedSupply = null;
            UpdateSelectedSupplyLabel();
        }

        SetDropdownListActive(false);
    }

    private void ClearSupplyOptionButtons()
    {
        foreach (Button button in _optionButtons)
        {
            if (button == null)
            {
                continue;
            }

            button.onClick.RemoveAllListeners();
            if (Application.isPlaying)
            {
                Destroy(button.gameObject);
            }
#if UNITY_EDITOR
            else
            {
                DestroyImmediate(button.gameObject);
            }
#endif
        }

        _optionButtons.Clear();
    }

    private void ToggleSupplyDropdown()
    {
        if (!HasSupplyOptions())
        {
            return;
        }

        SetDropdownListActive(!_dropdownVisible);
    }

    private void OnSupplyOptionSelected(SupplyDef supply)
    {
        _selectedSupply = supply;
        UpdateSelectedSupplyLabel();
        SetDropdownListActive(false);
    }

    private void UpdateSelectedSupplyLabel()
    {
        if (_supplyDropdownLabel == null)
        {
            return;
        }

        _supplyDropdownLabel.text = _selectedSupply == null ? DefaultSupplyLabel : GetSupplyDisplayName(_selectedSupply);
    }

    private string GetSupplyDisplayName(SupplyDef supply)
    {
        if (supply == null)
        {
            return "未知物资";
        }

        return !string.IsNullOrEmpty(supply.DisplayName) ? supply.DisplayName : supply.name;
    }

    private void SetDropdownListActive(bool active)
    {
        bool canShow = active && HasSupplyOptions() && _storageControlsRoot != null && _storageControlsRoot.activeInHierarchy;
        _dropdownVisible = canShow;
        if (_supplyDropdownListRoot != null)
        {
            _supplyDropdownListRoot.SetActive(canShow);
        }
    }
}