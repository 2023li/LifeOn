using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIItem_TechNode : MonoBehaviour
{
    [SerializeField,LabelText("当前节点")]
    private Button btn_TheNode;
    [SerializeField,LabelText("节点图标")]
    private Image img_TechIcon;
    [SerializeField,LabelText("节点名称")]
    private TMP_Text text_NodeName;
    [SerializeField,LabelText("研究进度条")]
    private Slider slider_ResearchProgress;
    [SerializeField,LabelText("节点描述")]
    private TMP_Text text_NodeDescription;

    [SerializeField,LabelText("连线入口点")]
    private RectTransform linePoint_Enter;
    [SerializeField,LabelText("连线出口")]
    private RectTransform linePoint_Export;

}
