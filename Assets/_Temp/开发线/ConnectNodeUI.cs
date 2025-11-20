using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ConnectNodeUI : MonoBehaviour
{
    [Header("Follow")]
    public Transform owner;
    public Vector3 worldOffset = new Vector3(0, 1.2f, 0);

    [Header("Start Point Config")]
    public bool isStartPoint = false;
    public Material startLineMaterial;
    public float startLineWidth = 0.06f;

    [Header("UI Refs")]
    public Canvas nodeCanvas;                 // World Space Canvas
    public Button mainPortButton;             // 折叠主端口
    public RectTransform mainPortRect;
    public RectTransform childPortsRoot;      // 子端口容器
    public Button collapseCatcherButton;      // 展开时用来折叠
    public RectTransform collapseCatcherRect;
    public NodePortUI childPortPrefab;        // 子端口 prefab

    [Header("Expand Layout")]
    public float expandRadius = 0.25f;

    public bool IsExpanded { get; private set; }

    private readonly List<ConnectionLine> _lines = new();
    private readonly Dictionary<ConnectionLine, NodePortUI> _childPorts = new();

    public IReadOnlyList<ConnectionLine> Lines => _lines;

    void Awake()
    {
        // 绑定点击切换
        mainPortButton.onClick.AddListener(Expand);
        collapseCatcherButton.onClick.AddListener(Collapse);

        Collapse(); // 初始折叠
    }

    void LateUpdate()
    {
        if (owner != null)
            transform.position = owner.position + worldOffset;
    }

    #region Visible
    public void SetVisible(bool visible)
    {
        nodeCanvas.enabled = visible;
        if (!visible) Collapse();
    }
    #endregion

    #region Material/Width from Start
    public Material GetStartMaterial(Material fallback)
        => startLineMaterial != null ? startLineMaterial : fallback;

    public float GetStartWidth(float fallback)
        => startLineWidth > 0 ? startLineWidth : fallback;
    #endregion

    #region Line Register
    public void RegisterLine(ConnectionLine line)
    {
        if (_lines.Contains(line)) return;
        _lines.Add(line);
        RefreshPorts();
    }

    public void UnregisterLine(ConnectionLine line)
    {
        if (_lines.Remove(line))
        {
            if (_childPorts.TryGetValue(line, out var port) && port != null)
                Destroy(port.gameObject);
            _childPorts.Remove(line);

            RefreshPorts();
        }
    }
    #endregion

    #region Expand/Collapse Toggle
    public void Toggle()
    {
        if (IsExpanded) Collapse();
        else Expand();
    }

    public void Expand()
    {
        // 只有多线时才需要展开；单线展开没意义
        if (_lines.Count <= 1) return;

        IsExpanded = true;

        mainPortButton.gameObject.SetActive(false);     // 隐藏主端口
        childPortsRoot.gameObject.SetActive(true);      // 显示子端口
        collapseCatcherButton.gameObject.SetActive(true);

        RefreshPorts();
    }

    public void Collapse()
    {
        IsExpanded = false;

        mainPortButton.gameObject.SetActive(true);      // 显示主端口
        childPortsRoot.gameObject.SetActive(false);     // 隐藏子端口
        collapseCatcherButton.gameObject.SetActive(false);

        // 销毁子端口
        foreach (var kv in _childPorts)
            if (kv.Value != null) Destroy(kv.Value.gameObject);
        _childPorts.Clear();
    }

    private void RefreshPorts()
    {
        if (!IsExpanded) return;

        if (_lines.Count <= 1)
        {
            Collapse();
            return;
        }

        int count = _lines.Count;

        // 创建/更新子端口，圆周均分
        for (int i = 0; i < count; i++)
        {
            var line = _lines[i];

            if (!_childPorts.TryGetValue(line, out var port) || port == null)
            {
                port = Instantiate(childPortPrefab, childPortsRoot);
                port.Init(this, line, isMain: false);
                _childPorts[line] = port;
            }

            float angle = (Mathf.PI * 2f / count) * i;
            Vector3 localPos = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * expandRadius;
            port.Rect.localPosition = localPos;
        }
    }

    public Transform GetAnchor(ConnectionLine line)
    {
        // 展开态该线挂对应子端口；折叠态都挂主端口
        if (IsExpanded && line != null && _childPorts.TryGetValue(line, out var port) && port != null)
            return port.transform;

        return mainPortRect.transform;
    }
    #endregion
}
