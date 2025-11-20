using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ConnectionManagerUI : MonoBehaviour
{
    public static ConnectionManagerUI I;

    [Header("Mode")]
    public bool connectionMode = false;

    [Header("Line Style Fallback")]
    public Material defaultMaterial;
    public float defaultWidth = 0.06f;
    public int lineSortingOrder = 10;

    [Header("Nodes")]
    public List<ConnectNodeUI> allNodes = new();

    private bool _dragging;
    private NodePortUI _startPort;
    private ConnectionLine _activeLine;
    private LineRenderer _tempLR;

    void Awake() => I = this;

    void Start() => SetMode(connectionMode);

    public void SetMode(bool on)
    {
        connectionMode = on;
        foreach (var n in allNodes)
            n.SetVisible(on);
    }

    // === Drag Flow ===
    public void StartDrag(NodePortUI startPort, PointerEventData e)
    {
        if (!connectionMode) return;

        _dragging = true;
        _startPort = startPort;

        // 子端口拖拽：指定某条线；主端口拖拽：可能是新线或该节点唯一线
        _activeLine = startPort.isMain
            ? (startPort.node.Lines.Count == 1 ? startPort.node.Lines[0] : null)
            : startPort.line;

        // 若是操作已有线且从中间节点开始 => BeginReroute
        if (_activeLine != null)
        {
            int idx = _activeLine.IndexOf(startPort.node);
            if (idx >= 0 && idx < _activeLine.nodes.Count - 1)
                _activeLine.BeginReroute(startPort.node);
        }

        CreateTempLine(startPort.transform.position);
        UpdateTempEnd(e.position);
    }

    public void Dragging(PointerEventData e)
    {
        if (!_dragging) return;
        UpdateTempEnd(e.position);
    }

    public void EndDrag(PointerEventData e)
    {
        if (!_dragging) return;

        var targetPort = RaycastPortUnderPointer(e);
        ConnectNodeUI targetNode = targetPort != null ? targetPort.node : null;

        if (targetNode != null && targetNode != _startPort.node)
        {
            if (_activeLine != null)
            {
                if (_activeLine.IsRerouting)
                    _activeLine.EndReroute(targetNode);
                else
                    _activeLine.AppendNode(targetNode);
            }
            else
            {
                CreateNewLine(_startPort.node, targetNode);
            }
        }
        else
        {
            if (_activeLine != null && _activeLine.IsRerouting)
                _activeLine.CancelReroute();
        }

        DestroyTempLine();
        _dragging = false;
        _startPort = null;
        _activeLine = null;
    }

    // === UI Raycast ===
    private NodePortUI RaycastPortUnderPointer(PointerEventData e)
    {
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(e, results);

        foreach (var r in results)
        {
            var port = r.gameObject.GetComponentInParent<NodePortUI>();
            if (port != null) return port;
        }
        return null;
    }

    // === Line Create ===
    private void CreateNewLine(ConnectNodeUI a, ConnectNodeUI b)
    {
        if (!a.isStartPoint) return; // 起点限制

        var go = new GameObject("ConnectionLine");
        var line = go.AddComponent<ConnectionLine>();

        var mat = a.GetStartMaterial(defaultMaterial);
        var width = a.GetStartWidth(defaultWidth);

        line.Init(mat, width, lineSortingOrder);
        line.AddStart(a);
        line.AppendNode(b);
    }

    // === Temp Line ===
    private void CreateTempLine(Vector3 startWorldPos)
    {
        var go = new GameObject("TempLine");
        _tempLR = go.AddComponent<LineRenderer>();
        _tempLR.material = defaultMaterial;
        _tempLR.startWidth = defaultWidth;
        _tempLR.endWidth = defaultWidth;
        _tempLR.useWorldSpace = true;
        _tempLR.positionCount = 2;
        _tempLR.SetPosition(0, startWorldPos);
        _tempLR.SetPosition(1, startWorldPos);
        _tempLR.sortingOrder = lineSortingOrder + 1;
    }

    private void UpdateTempEnd(Vector2 screenPos)
    {
        if (_tempLR == null) return;

        Vector3 wpos = Camera.main.ScreenToWorldPoint(screenPos);
        wpos.z = 0;

        _tempLR.SetPosition(1, wpos);
    }

    private void DestroyTempLine()
    {
        if (_tempLR != null)
            Destroy(_tempLR.gameObject);
        _tempLR = null;
    }
}
