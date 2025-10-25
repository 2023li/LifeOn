using System;
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


    private TechNodeData _data;
    private TechTreeManager _manager;
    private Action<string> _onRequestResearch;

    public RectTransform LinePointEnter => linePoint_Enter;
    public RectTransform LinePointExport => linePoint_Export;

    public string NodeId => _data != null ? _data.id : string.Empty;

    public void Bind(TechNodeData data, TechTreeManager manager, Action<string> onRequestResearch)
    {
        _data = data;
        _manager = manager;
        _onRequestResearch = onRequestResearch;

        if (btn_TheNode != null)
        {
            btn_TheNode.onClick.RemoveListener(OnNodeButtonClicked);
            btn_TheNode.onClick.AddListener(OnNodeButtonClicked);
        }

        if (text_NodeName != null)
        {
            text_NodeName.text = _data != null ? _data.name : string.Empty;
        }

        if (text_NodeDescription != null)
        {
            text_NodeDescription.text = _data != null ? _data.description : string.Empty;
        }

        if (img_TechIcon != null)
        {
            img_TechIcon.sprite = _data != null ? _data.icon : null;
            img_TechIcon.enabled = img_TechIcon.sprite != null;
        }
    }

    public void Refresh(bool canResearch, bool isResearching, float progress, bool isUnlocked)
    {
        if (_data == null)
        {
            return;
        }

        if (slider_ResearchProgress != null)
        {
            float clampedProgress = Mathf.Clamp01(progress);
            slider_ResearchProgress.value = isUnlocked ? 1f : clampedProgress;
            bool showProgress = isResearching || isUnlocked || clampedProgress > 0f;
            slider_ResearchProgress.gameObject.SetActive(showProgress);
        }

        if (btn_TheNode != null)
        {
            bool interactable = canResearch && !isUnlocked && _manager != null;
            btn_TheNode.interactable = interactable;
        }
    }

    private void OnNodeButtonClicked()
    {
        if (_data == null)
        {
            return;
        }

        _onRequestResearch?.Invoke(_data.id);
    }

    private void OnDestroy()
    {
        if (btn_TheNode != null)
        {
            btn_TheNode.onClick.RemoveListener(OnNodeButtonClicked);
        }
    }

}
