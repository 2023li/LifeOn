using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugManager : MonoBehaviour
{
    [Header("是否显示鼠标所在单元格坐标")]
    public bool b_显示鼠标位置的坐标;

    [Header("字体设置")]
    public int fontSize = 20; // 可以调整这个值来改变字体大小

    private GUIStyle _guiStyle;

    private void Awake()
    {
        // 创建自定义的 GUIStyle
        _guiStyle = new GUIStyle();
        _guiStyle.fontSize = fontSize;
        _guiStyle.normal.textColor = Color.white;
        _guiStyle.normal.background = MakeTex(2, 2, new Color(0f, 0f, 0f, 0.7f)); // 半透明黑色背景
        _guiStyle.alignment = TextAnchor.MiddleCenter;
        _guiStyle.padding = new RectOffset(5, 5, 5, 5);
    }

    private void OnGUI()
    {
        Show_鼠标坐标();
    }

    private void Show_鼠标坐标()
    {
        if (!b_显示鼠标位置的坐标)
            return;

        if (GridSystem.Instance == null)
            return;

        // 获取鼠标所在单元格坐标（网格坐标）
        Vector3Int cellCoor = GridSystem.Instance.GetMousePosCoordinates();

        // 把鼠标屏幕坐标转换到 GUI 坐标系（Y 轴反向）
        Vector3 mousePos = Input.mousePosition;
        mousePos.y = Screen.height - mousePos.y;

        // 要显示的文本内容，可以根据需要调整格式
        string text = $"Cell: ({cellCoor.x}, {cellCoor.y}, {cellCoor.z})";

        // 根据字体大小调整框的大小
        float width = 150f * (fontSize / 20f); // 根据字体大小缩放宽度
        float height = 25f * (fontSize / 20f); // 根据字体大小缩放高度
        Rect rect = new Rect(mousePos.x + 15f, mousePos.y + 15f, width, height);

        GUI.Box(rect, text, _guiStyle);
    }

    // 创建一个纯色纹理用于背景
    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++)
        {
            pix[i] = col;
        }
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }

    // 可选：在Inspector中修改fontSize时实时更新样式
    private void OnValidate()
    {
        if (_guiStyle != null)
        {
            _guiStyle.fontSize = fontSize;
        }
    }
}
