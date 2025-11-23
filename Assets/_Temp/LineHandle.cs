using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LineHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public ConnectionLine line;
    public UINode ownerNode;

    public void Init(ConnectionLine l, UINode owner)
    {
        line = l;
        ownerNode = owner;

        // 让 handle 可被射线点击
        var img = gameObject.AddComponent<Image>();
        img.raycastTarget = true;
        img.color = Color.black; // 你可以改成和线材质一致的颜色

        var cg = gameObject.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = true;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        ConnectionManager.I.StartDragHandle(line, ownerNode, eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        ConnectionManager.I.Drag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        ConnectionManager.I.EndDrag(ownerNode, eventData);
    }
}
