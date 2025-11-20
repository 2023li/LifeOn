using UnityEngine;
using UnityEngine.EventSystems;

public class NodePortUI : MonoBehaviour,
    IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public RectTransform Rect { get; private set; }
    public ConnectNodeUI node { get; private set; }
    public ConnectionLine line { get; private set; }
    public bool isMain { get; private set; }

    void Awake() => Rect = GetComponent<RectTransform>();

    public void Init(ConnectNodeUI n, ConnectionLine l, bool isMain)
    {
        node = n;
        line = l;
        this.isMain = isMain;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // 主端口点击：折叠 -> 展开
        // 展开态折叠由 CollapseCatcher 负责
        if (isMain && node.Lines.Count > 1)
        {
            node.Expand();
            eventData.Use();
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // 起点限制：只有主端口且是起点才能新建线
        if (isMain && !node.isStartPoint && node.Lines.Count == 0)
            return;

        ConnectionManagerUI.I.StartDrag(this, eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        ConnectionManagerUI.I.Dragging(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        ConnectionManagerUI.I.EndDrag(eventData);
    }
}
