using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

public class UIItem_TechPanel : MonoBehaviour
{
    [SerializeField, LabelText("科技节点列表")]
    private List<UIItem_TechNode> techNodes = new List<UIItem_TechNode>();

    private TechTreeManager _techTree;

    private void Awake()
    {
        EnsureManager();
    }

    private void OnEnable()
    {
        RefreshTechNodes();
    }

    public void OnRequestResearch(string techId)
    {
        if (string.IsNullOrWhiteSpace(techId))
        {
            return;
        }

        EnsureManager();
        if (_techTree == null)
        {
            return;
        }

        if (_techTree.SetActiveResearch(techId))
        {
            RefreshTechNodes();
        }
    }

    public void RefreshTechNodes()
    {
        EnsureManager();
        if (_techTree == null)
        {
            return;
        }

        var activeId = _techTree.ActiveResearchId;
        var availableSet = new HashSet<string>(
            _techTree.GetResearchableNodes().Select(n => n.id),
            StringComparer.OrdinalIgnoreCase);

        foreach (var node in techNodes)
        {
            if (node == null)
            {
                continue;
            }

            var id = node.NodeId;
            if (string.IsNullOrEmpty(id))
            {
                continue;
            }

            bool isUnlocked = _techTree.IsUnlocked(id);
            bool isActive = !string.IsNullOrEmpty(activeId) &&
                            activeId.Equals(id, StringComparison.OrdinalIgnoreCase);
            bool canResearch = !isUnlocked &&
                               (availableSet.Contains(id) || _techTree.IsResearching(id));
            float progress = _techTree.GetResearchProgress(id);

            node.Refresh(canResearch, isActive, progress, isUnlocked);
        }
    }

    private void EnsureManager()
    {
        if (_techTree == null)
        {
            _techTree = GameContext.Instance?.TechTree;
        }
    }
}
