using Moyo.Unity;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

public class UIItem_HaloPanel : MonoBehaviour
{
    [AutoBind] public Toggle toggle_环境;
    [AutoBind] public Toggle toggle_治安;
    [AutoBind] public Toggle toggle_医疗;

    [SerializeField, LabelText("网格系统")]
    private GridSystem gridSystem;

    [SerializeField, LabelText("游戏上下文")]
    private GameContext gameContext;

    private IGameContext _cachedContext;
    private AuraCategory? _lastRequestedCategory;

    private void Reset()
    {
        this.AutoBindFields();
    }

    private void Awake()
    {
        if (toggle_环境 != null)
        {
            toggle_环境.onValueChanged.AddListener(OnEnvironmentToggleChanged);
        }

        if (toggle_治安 != null)
        {
            toggle_治安.onValueChanged.AddListener(OnSecurityToggleChanged);
        }

        if (toggle_医疗 != null)
        {
            toggle_医疗.onValueChanged.AddListener(OnHealthToggleChanged);
        }
    }

    private void OnEnable()
    {
        RefreshHighlight();
    }

    private void OnDestroy()
    {
        if (toggle_环境 != null)
        {
            toggle_环境.onValueChanged.RemoveListener(OnEnvironmentToggleChanged);
        }

        if (toggle_治安 != null)
        {
            toggle_治安.onValueChanged.RemoveListener(OnSecurityToggleChanged);
        }

        if (toggle_医疗 != null)
        {
            toggle_医疗.onValueChanged.RemoveListener(OnHealthToggleChanged);
        }
    }

    private void OnEnvironmentToggleChanged(bool isOn)
    {
        HandleToggleChanged(AuraCategory.Beauty, isOn);
    }

    private void OnSecurityToggleChanged(bool isOn)
    {
        HandleToggleChanged(AuraCategory.Security, isOn);
    }

    private void OnHealthToggleChanged(bool isOn)
    {
        HandleToggleChanged(AuraCategory.Health, isOn);
    }

    private void HandleToggleChanged(AuraCategory category, bool isOn)
    {
        if (isOn)
        {
            _lastRequestedCategory = category;
            ShowCategory(category);
        }
        else
        {
            RefreshHighlight();
        }
    }

    private void RefreshHighlight()
    {
        GridSystem grid = Grid;
        if (grid == null)
        {
            return;
        }

        AuraCategory? target = null;
        if (_lastRequestedCategory.HasValue && IsToggleOn(_lastRequestedCategory.Value))
        {
            target = _lastRequestedCategory.Value;
        }
        else
        {
            if (toggle_环境 != null && toggle_环境.isOn)
            {
                target = AuraCategory.Beauty;
            }
            else if (toggle_治安 != null && toggle_治安.isOn)
            {
                target = AuraCategory.Security;
            }
            else if (toggle_医疗 != null && toggle_医疗.isOn)
            {
                target = AuraCategory.Health;
            }
        }

        if (target.HasValue)
        {
            _lastRequestedCategory = target;
            grid.ShowAuraHighlight(GetContext(), target.Value);
        }
        else
        {
            _lastRequestedCategory = null;
            grid.ClearHighlight();
        }
    }

    private bool IsToggleOn(AuraCategory category)
    {
        switch (category)
        {
            case AuraCategory.Beauty:
                return toggle_环境 != null && toggle_环境.isOn;
            case AuraCategory.Security:
                return toggle_治安 != null && toggle_治安.isOn;
            case AuraCategory.Health:
                return toggle_医疗 != null && toggle_医疗.isOn;
            default:
                return false;
        }
    }

    private void ShowCategory(AuraCategory category)
    {
        GridSystem grid = Grid;
        if (grid == null)
        {
            return;
        }

        grid.ShowAuraHighlight(GetContext(), category);
    }

    private GridSystem Grid
    {
        get
        {
            if (gridSystem == null)
            {
                gridSystem = GridSystem.Instance;
            }

            return gridSystem;
        }
    }

    private IGameContext GetContext()
    {
        if (_cachedContext != null)
        {
            return _cachedContext;
        }

        if (gameContext != null)
        {
            _cachedContext = gameContext;
            return _cachedContext;
        }

        _cachedContext = FindObjectOfType<GameContext>();
        return _cachedContext;
    }
}
