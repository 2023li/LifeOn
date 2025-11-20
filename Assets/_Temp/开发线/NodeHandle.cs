using UnityEngine;

// 展开后的子连线点：每个点对应一条线
public class NodeHandle : MonoBehaviour
{
    public ConnectNodeUI ownerNode { get; private set; }
    public ConnectionLine line { get; private set; }

    public void Init(ConnectNodeUI node, ConnectionLine l)
    {
        ownerNode = node;
        line = l;
    }
}
