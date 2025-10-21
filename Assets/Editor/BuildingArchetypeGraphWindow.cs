using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using static BuildingArchetypeGraph;

/// <summary>
/// 建筑原型图编辑窗口，负责管理 BuildingArchetypeGraph 的图形化编辑。
/// </summary>
public class BuildingArchetypeGraphWindow : EditorWindow
{
    private const string WindowTitle = "Building Archetype Graph";

    [SerializeField] private BuildingArchetypeGraph _graphAsset;
    [SerializeField] private BuildingArchetype _archetypeAsset;

    private SerializedObject _graphSerializedObject;
    private BuildingArchetypeGraphView _graphView;
    private VisualElement _inspectorPanel;
    private Label _inspectorHeader;

    [MenuItem("LifeOn/Building Archetype Graph")]
    public static void OpenWindow()
    {
        var window = GetWindow<BuildingArchetypeGraphWindow>();
        window.titleContent = new GUIContent(WindowTitle);
        window.Show();
    }

    private void OnEnable()
    {
        Undo.undoRedoPerformed += HandleUndoRedo;
        ConstructUI();
        RefreshGraphView();
    }

    private void OnDisable()
    {
        Undo.undoRedoPerformed -= HandleUndoRedo;

        if (_graphView != null)
        {
            rootVisualElement.Remove(_graphView);
        }
    }

    private void ConstructUI()
    {
        rootVisualElement.style.flexDirection = FlexDirection.Column;
        rootVisualElement.style.flexGrow = 1f;

        var toolbar = new Toolbar();
        toolbar.style.flexShrink = 0f;
        BuildToolbar(toolbar);
        rootVisualElement.Add(toolbar);

        var contentSplit = new TwoPaneSplitView(0, 250f, TwoPaneSplitViewOrientation.Horizontal);
        rootVisualElement.Add(contentSplit);

        _graphView = new BuildingArchetypeGraphView(this)
        {
            name = "BuildingArchetypeGraphView"
        };
        contentSplit.Add(_graphView);

        var inspectorContainer = new VisualElement
        {
            style =
            {
                flexDirection = FlexDirection.Column,
                flexGrow = 1f
            }
        };

        _inspectorHeader = new Label("Inspector")
        {
            style =
            {
                unityFontStyleAndWeight = FontStyle.Bold,
                unityTextAlign = TextAnchor.MiddleCenter,
                unityFontSize = 12,
                paddingTop = 4,
                paddingBottom = 4
            }
        };
        inspectorContainer.Add(_inspectorHeader);

        _inspectorPanel = new ScrollView();
        _inspectorPanel.style.flexGrow = 1f;
        inspectorContainer.Add(_inspectorPanel);

        contentSplit.Add(inspectorContainer);

        rootVisualElement.style.flexGrow = 1f;
    }

    private void BuildToolbar(Toolbar toolbar)
    {
        // 选择图资产
        var graphField = new ObjectField("Graph")
        {
            objectType = typeof(BuildingArchetypeGraph),
            allowSceneObjects = false,
            value = _graphAsset
        };
        graphField.RegisterValueChangedCallback(evt =>
        {
            if (evt.newValue == evt.previousValue)
            {
                return;
            }

            _graphAsset = evt.newValue as BuildingArchetypeGraph;
            PrepareGraphSerializedObject();
            RefreshGraphView();
        });
        toolbar.Add(graphField);

        // 选择目标原型资产
        var archetypeField = new ObjectField("Archetype")
        {
            objectType = typeof(BuildingArchetype),
            allowSceneObjects = false,
            value = _archetypeAsset
        };
        archetypeField.RegisterValueChangedCallback(evt => { _archetypeAsset = evt.newValue as BuildingArchetype; });
        toolbar.Add(archetypeField);

        toolbar.Add(new ToolbarButton(() =>
        {
            if (_graphAsset == null)
            {
                EditorUtility.DisplayDialog("提示", "请先选择 BuildingArchetypeGraph 资产。", "好的");
                return;
            }

            string path = EditorUtility.SaveFilePanelInProject("创建 BuildingArchetypeGraph", "NewBuildingArchetypeGraph", "asset", "请选择保存位置");
            if (!string.IsNullOrEmpty(path))
            {
                var graph = CreateInstance<BuildingArchetypeGraph>();
                AssetDatabase.CreateAsset(graph, path);
                AssetDatabase.SaveAssets();
                _graphAsset = graph;
                graphField.value = graph;
                PrepareGraphSerializedObject();
                RefreshGraphView();
            }
        })
        {
            text = "新建图"
        });

        toolbar.Add(new ToolbarButton(SaveToArchetype)
        {
            text = "保存"
        });

        toolbar.Add(new ToolbarButton(ImportFromArchetype)
        {
            text = "导入"
        });

        toolbar.Add(new ToolbarButton(() =>
        {
            _graphView.FrameAll();
        })
        {
            text = "框选全部"
        });

        toolbar.Add(new ToolbarButton(() =>
        {
            _graphView.ResetGraphView();
        })
        {
            text = "重置缩放"
        });

        var createMenu = new ToolbarMenu { text = "创建节点" };
        createMenu.menu.AppendAction("等级", _ => _graphView.CreateNode(NodeCategory.Level));
        createMenu.menu.AppendAction("规则", _ => _graphView.CreateNode(NodeCategory.Rule));
        createMenu.menu.AppendAction("条件", _ => _graphView.CreateNode(NodeCategory.Condition));
        createMenu.menu.AppendAction("效果/成功", _ => _graphView.CreateNode(NodeCategory.EffectSuccess));
        createMenu.menu.AppendAction("效果/失败", _ => _graphView.CreateNode(NodeCategory.EffectFailure));
        createMenu.menu.AppendAction("属性修正", _ => _graphView.CreateNode(NodeCategory.StatModifier));
        toolbar.Add(createMenu);
    }

    private void PrepareGraphSerializedObject()
    {
        if (_graphAsset != null)
        {
            _graphSerializedObject = new SerializedObject(_graphAsset);
        }
        else
        {
            _graphSerializedObject = null;
        }
    }

    private void HandleUndoRedo()
    {
        if (_graphAsset != null)
        {
            RefreshGraphView();
        }
    }

    private void RefreshGraphView()
    {
        if (_graphAsset == null)
        {
            _graphView?.ClearGraph();
            _inspectorPanel.Clear();
            _inspectorHeader.text = "Inspector";
            return;
        }

        if (_graphSerializedObject == null)
        {
            PrepareGraphSerializedObject();
        }

        _graphSerializedObject?.UpdateIfRequiredOrScript();
        _graphView?.Populate(_graphAsset, _graphSerializedObject);
    }

    private void SaveToArchetype()
    {
        if (_graphAsset == null)
        {
            EditorUtility.DisplayDialog("提示", "请先选择 BuildingArchetypeGraph 资产。", "好的");
            return;
        }

        if (_archetypeAsset == null)
        {
            EditorUtility.DisplayDialog("提示", "请为保存指定 BuildingArchetype 资产。", "好的");
            return;
        }

        RecordGraphChange("保存建筑原型");
        _graphAsset.ToArchetype(_archetypeAsset);
        EditorUtility.SetDirty(_archetypeAsset);
        AssetDatabase.SaveAssets();
    }

    private void ImportFromArchetype()
    {
        if (_graphAsset == null)
        {
            EditorUtility.DisplayDialog("提示", "请先选择 BuildingArchetypeGraph 资产。", "好的");
            return;
        }

        if (_archetypeAsset == null)
        {
            EditorUtility.DisplayDialog("提示", "请指定 BuildingArchetype 资产用于导入。", "好的");
            return;
        }

        RecordGraphChange("导入建筑原型");
        _graphAsset.FromArchetype(_archetypeAsset);
        PrepareGraphSerializedObject();
        RefreshGraphView();
        EditorUtility.SetDirty(_graphAsset);
    }

    public void RecordGraphChange(string description)
    {
        if (_graphAsset == null)
        {
            return;
        }

        Undo.RecordObject(_graphAsset, description);
        EditorUtility.SetDirty(_graphAsset);
    }

    public void ShowInspector(GraphNodeView nodeView)
    {
        _inspectorPanel.Clear();
        if (nodeView == null)
        {
            _inspectorHeader.text = "Inspector";
            return;
        }

        _graphSerializedObject?.UpdateIfRequiredOrScript();
        nodeView.RefreshTitle();
        _inspectorHeader.text = nodeView.title;
        var element = nodeView.CreateInspectorElement(_graphSerializedObject);
        if (element != null)
        {
            _inspectorPanel.Add(element);
        }
    }

    public BuildingArchetypeGraph GraphAsset => _graphAsset;
    public SerializedObject GraphSerializedObject => _graphSerializedObject;
}

/// <summary>
/// 图节点所属分类，用于创建端口及建立连线逻辑。
/// </summary>
public enum NodeCategory
{
    BuildingInfo,
    Level,
    Rule,
    Condition,
    EffectSuccess,
    EffectFailure,
    StatModifier
}

/// <summary>
/// 连线类型，决定更新数据时的目标字段。
/// </summary>
public enum LinkCategory
{
    BuildingToLevel,
    LevelToRule,
    LevelToStatModifier,
    RuleToCondition,
    RuleToSuccessEffect,
    RuleToFailureEffect
}

/// <summary>
/// 建筑原型图视图核心逻辑。
/// </summary>
public class BuildingArchetypeGraphView : GraphView
{
    private readonly BuildingArchetypeGraphWindow _window;
    private readonly Dictionary<string, GraphNodeView> _nodeLookup = new();

    private BuildingArchetypeGraph _graphAsset;
    private SerializedObject _graphSerializedObject;
    private bool _isPopulating;

    public BuildingArchetypeGraphView(BuildingArchetypeGraphWindow window)
    {
        _window = window;

        style.flexGrow = 1f;
        SetupZoom(0.05f, 4f, 0.05f);
        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());

        var grid = new GridBackground();
        Insert(0, grid);

        graphViewChanged += OnGraphViewChanged;
        nodeCreationRequest = ctx =>
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("等级"), false, () => CreateNode(NodeCategory.Level, ctx.screenMousePosition));
            menu.AddItem(new GUIContent("规则"), false, () => CreateNode(NodeCategory.Rule, ctx.screenMousePosition));
            menu.AddItem(new GUIContent("条件"), false, () => CreateNode(NodeCategory.Condition, ctx.screenMousePosition));
            menu.AddItem(new GUIContent("效果/成功"), false, () => CreateNode(NodeCategory.EffectSuccess, ctx.screenMousePosition));
            menu.AddItem(new GUIContent("效果/失败"), false, () => CreateNode(NodeCategory.EffectFailure, ctx.screenMousePosition));
            menu.AddItem(new GUIContent("属性修正"), false, () => CreateNode(NodeCategory.StatModifier, ctx.screenMousePosition));
            menu.ShowAsContext();
        };

        selectionChanged += OnSelectionChanged;
    }

    public void ClearGraph()
    {
        var removable = graphElements.Where(element => element is Edge || element is Node).ToList();
        DeleteElements(removable);
        _nodeLookup.Clear();
    }

    public void Populate(BuildingArchetypeGraph graph, SerializedObject serialized)
    {
        _graphAsset = graph;
        _graphSerializedObject = serialized;

        _isPopulating = true;
        ClearGraph();

        if (_graphAsset == null || _graphSerializedObject == null)
        {
            _isPopulating = false;
            return;
        }

        EnsureNodeIds();

        AddBuildingInfoNode();
        AddLevelNodes();
        AddRuleNodes();
        AddConditionNodes();
        AddEffectNodes();
        AddStatModifierNodes();

        BuildEdges();
        _isPopulating = false;
    }

    public void ResetGraphView()
    {
        UpdateViewTransform(Vector3.zero, Vector3.one);
    }

    private void EnsureNodeIds()
    {
        _graphAsset.BuildingInfo?.ForceSetIdIfEmpty();

        foreach (var level in _graphAsset.Levels)
        {
            level.ForceSetIdIfEmpty();
        }

        foreach (var rule in _graphAsset.Rules)
        {
            rule.ForceSetIdIfEmpty();
        }

        foreach (var condition in _graphAsset.Conditions)
        {
            condition.ForceSetIdIfEmpty();
        }

        foreach (var effect in _graphAsset.Effects)
        {
            effect.ForceSetIdIfEmpty();
        }

        foreach (var modifier in _graphAsset.StatModifiers)
        {
            modifier.ForceSetIdIfEmpty();
        }
    }

    private void AddBuildingInfoNode()
    {
        if (_graphAsset.BuildingInfo == null)
        {
            return;
        }

        var property = _graphSerializedObject.FindProperty("_buildingInfo");
        var node = new BuildingInfoNodeView(_window, property.propertyPath, _graphAsset.BuildingInfo);
        AddElement(node);
        _nodeLookup[_graphAsset.BuildingInfo.Id] = node;
    }

    private void AddLevelNodes()
    {
        var levelsProperty = _graphSerializedObject.FindProperty("_levels");
        for (int i = 0; i < _graphAsset.Levels.Count; i++)
        {
            var data = _graphAsset.Levels[i];
            var property = levelsProperty.GetArrayElementAtIndex(i);
            var node = new LevelNodeView(_window, property.propertyPath, data);
            AddElement(node);
            _nodeLookup[data.Id] = node;
        }
    }

    private void AddRuleNodes()
    {
        var property = _graphSerializedObject.FindProperty("_rules");
        for (int i = 0; i < _graphAsset.Rules.Count; i++)
        {
            var data = _graphAsset.Rules[i];
            var element = property.GetArrayElementAtIndex(i);
            var node = new RuleNodeView(_window, element.propertyPath, data);
            AddElement(node);
            _nodeLookup[data.Id] = node;
        }
    }

    private void AddConditionNodes()
    {
        var property = _graphSerializedObject.FindProperty("_conditions");
        for (int i = 0; i < _graphAsset.Conditions.Count; i++)
        {
            var data = _graphAsset.Conditions[i];
            var element = property.GetArrayElementAtIndex(i);
            var node = new ConditionNodeView(_window, element.propertyPath, data);
            AddElement(node);
            _nodeLookup[data.Id] = node;
        }
    }

    private void AddEffectNodes()
    {
        var property = _graphSerializedObject.FindProperty("_effects");
        for (int i = 0; i < _graphAsset.Effects.Count; i++)
        {
            var data = _graphAsset.Effects[i];
            var element = property.GetArrayElementAtIndex(i);
            var node = new EffectNodeView(_window, element.propertyPath, data);
            AddElement(node);
            _nodeLookup[data.Id] = node;
        }
    }

    private void AddStatModifierNodes()
    {
        var property = _graphSerializedObject.FindProperty("_statModifiers");
        for (int i = 0; i < _graphAsset.StatModifiers.Count; i++)
        {
            var data = _graphAsset.StatModifiers[i];
            var element = property.GetArrayElementAtIndex(i);
            var node = new StatModifierNodeView(_window, element.propertyPath, data);
            AddElement(node);
            _nodeLookup[data.Id] = node;
        }
    }

    private void BuildEdges()
    {
        if (_graphAsset.BuildingInfo != null && _nodeLookup.TryGetValue(_graphAsset.BuildingInfo.Id, out var buildingNode))
        {
            foreach (var levelId in _graphAsset.BuildingInfo.LevelNodeIds)
            {
                if (_nodeLookup.TryGetValue(levelId, out var levelNode))
                {
                    var output = buildingNode.GetPort(LinkCategory.BuildingToLevel, Direction.Output);
                    var input = levelNode.GetPort(LinkCategory.BuildingToLevel, Direction.Input);
                    if (output != null && input != null)
                    {
                        var edge = output.ConnectTo(input);
                        AddElement(edge);
                    }
                }
            }
        }

        foreach (var level in _graphAsset.Levels)
        {
            if (!_nodeLookup.TryGetValue(level.Id, out var levelNode))
            {
                continue;
            }

            foreach (var ruleId in level.RuleNodeIds)
            {
                if (_nodeLookup.TryGetValue(ruleId, out var ruleNode))
                {
                    var output = levelNode.GetPort(LinkCategory.LevelToRule, Direction.Output);
                    var input = ruleNode.GetPort(LinkCategory.LevelToRule, Direction.Input);
                    if (output != null && input != null)
                    {
                        var edge = output.ConnectTo(input);
                        AddElement(edge);
                    }
                }
            }

            foreach (var modifierId in level.StatModifierNodeIds)
            {
                if (_nodeLookup.TryGetValue(modifierId, out var modifierNode))
                {
                    var output = levelNode.GetPort(LinkCategory.LevelToStatModifier, Direction.Output);
                    var input = modifierNode.GetPort(LinkCategory.LevelToStatModifier, Direction.Input);
                    if (output != null && input != null)
                    {
                        var edge = output.ConnectTo(input);
                        AddElement(edge);
                    }
                }
            }
        }

        foreach (var rule in _graphAsset.Rules)
        {
            if (!_nodeLookup.TryGetValue(rule.Id, out var ruleNode))
            {
                continue;
            }

            foreach (var conditionId in rule.ConditionNodeIds)
            {
                if (_nodeLookup.TryGetValue(conditionId, out var conditionNode))
                {
                    var output = ruleNode.GetPort(LinkCategory.RuleToCondition, Direction.Output);
                    var input = conditionNode.GetPort(LinkCategory.RuleToCondition, Direction.Input);
                    if (output != null && input != null)
                    {
                        var edge = output.ConnectTo(input);
                        AddElement(edge);
                    }
                }
            }

            foreach (var successId in rule.SuccessEffectNodeIds)
            {
                if (_nodeLookup.TryGetValue(successId, out var effectNode))
                {
                    if (effectNode is EffectNodeView effectView)
                    {
                        effectView.UpdateSlot(true);
                    }
                    var output = ruleNode.GetPort(LinkCategory.RuleToSuccessEffect, Direction.Output);
                    var input = effectNode.GetPort(LinkCategory.RuleToSuccessEffect, Direction.Input);
                    if (output != null && input != null)
                    {
                        var edge = output.ConnectTo(input);
                        AddElement(edge);
                    }
                }
            }

            foreach (var failureId in rule.FailureEffectNodeIds)
            {
                if (_nodeLookup.TryGetValue(failureId, out var effectNode))
                {
                    if (effectNode is EffectNodeView effectView)
                    {
                        effectView.UpdateSlot(false);
                    }
                    var output = ruleNode.GetPort(LinkCategory.RuleToFailureEffect, Direction.Output);
                    var input = effectNode.GetPort(LinkCategory.RuleToFailureEffect, Direction.Input);
                    if (output != null && input != null)
                    {
                        var edge = output.ConnectTo(input);
                        AddElement(edge);
                    }
                }
            }
        }
    }

    private GraphViewChange OnGraphViewChanged(GraphViewChange change)
    {
        if (_isPopulating)
        {
            return change;
        }

        if (change.edgesToCreate != null)
        {
            foreach (var edge in change.edgesToCreate)
            {
                HandleEdgeCreated(edge);
            }
        }

        if (change.elementsToRemove != null)
        {
            foreach (var element in change.elementsToRemove)
            {
                switch (element)
                {
                    case Edge edge:
                        HandleEdgeRemoved(edge);
                        break;
                    case GraphNodeView nodeView:
                        RemoveNodeData(nodeView);
                        break;
                }
            }
        }

        return change;
    }

    private void OnSelectionChanged(IEnumerable<GraphElement> selection)
    {
        var nodeView = selection?.OfType<GraphNodeView>().FirstOrDefault();
        _window.ShowInspector(nodeView);
    }

    public void CreateNode(NodeCategory category, Vector2? screenPosition = null)
    {
        if (_graphAsset == null)
        {
            return;
        }

        Vector2 graphPosition = screenPosition.HasValue
            ? contentViewContainer.WorldToLocal(screenPosition.Value)
            : new Vector2(100f, 100f);

        string createdId = null;

        _window.RecordGraphChange("创建节点");

        switch (category)
        {
            case NodeCategory.Level:
                var level = new LevelNodeData();
                level.ForceSetIdIfEmpty();
                level.DisplayName = "Level";
                level.SetRuntimeType(typeof(BuildingLevelDef));
                createdId = level.Id;
                _graphAsset.BuildingInfo?.LevelNodeIds.Add(level.Id);
                _graphAsset.Levels.Add(level);
                break;
            case NodeCategory.Rule:
                var rule = new RuleNodeData();
                rule.ForceSetIdIfEmpty();
                rule.DisplayName = "Rule";
                rule.SetRuntimeType(typeof(Rule));
                createdId = rule.Id;
                _graphAsset.Rules.Add(rule);
                break;
            case NodeCategory.Condition:
                var condition = new ConditionNodeData();
                condition.ForceSetIdIfEmpty();
                condition.DisplayName = "Condition";
                createdId = condition.Id;
                _graphAsset.Conditions.Add(condition);
                break;
            case NodeCategory.EffectSuccess:
                var success = new EffectNodeData();
                success.ForceSetIdIfEmpty();
                success.DisplayName = "Effect";
                success.Slot = EffectNodeData.EffectSlot.Success;
                createdId = success.Id;
                _graphAsset.Effects.Add(success);
                break;
            case NodeCategory.EffectFailure:
                var failure = new EffectNodeData();
                failure.ForceSetIdIfEmpty();
                failure.DisplayName = "Effect";
                failure.Slot = EffectNodeData.EffectSlot.Failure;
                createdId = failure.Id;
                _graphAsset.Effects.Add(failure);
                break;
            case NodeCategory.StatModifier:
                var modifier = new StatModifierNodeData();
                modifier.ForceSetIdIfEmpty();
                modifier.DisplayName = "StatModifier";
                createdId = modifier.Id;
                _graphAsset.StatModifiers.Add(modifier);
                break;
            default:
                break;
        }

        _window.GraphSerializedObject?.UpdateIfRequiredOrScript();
        Populate(_graphAsset, _window.GraphSerializedObject);

        if (createdId != null && _nodeLookup.TryGetValue(createdId, out var createdNode))
        {
            createdNode.SetPosition(new Rect(graphPosition, new Vector2(280f, 160f)));
        }
    }

    private void HandleEdgeCreated(Edge edge)
    {
        if (edge.output?.userData is not PortMetadata outputMeta || edge.input?.userData is not PortMetadata inputMeta)
        {
            return;
        }

        _window.RecordGraphChange("连接节点");

        switch (outputMeta.Link)
        {
            case LinkCategory.BuildingToLevel:
                ConnectBuildingToLevel(outputMeta.Node, inputMeta.Node);
                break;
            case LinkCategory.LevelToRule:
                ConnectLevelToRule(outputMeta.Node, inputMeta.Node);
                break;
            case LinkCategory.LevelToStatModifier:
                ConnectLevelToModifier(outputMeta.Node, inputMeta.Node);
                break;
            case LinkCategory.RuleToCondition:
                ConnectRuleToCondition(outputMeta.Node, inputMeta.Node);
                break;
            case LinkCategory.RuleToSuccessEffect:
                ConnectRuleToEffect(outputMeta.Node, inputMeta.Node, true);
                break;
            case LinkCategory.RuleToFailureEffect:
                ConnectRuleToEffect(outputMeta.Node, inputMeta.Node, false);
                break;
        }

        _window.GraphSerializedObject?.UpdateIfRequiredOrScript();
    }

    private void HandleEdgeRemoved(Edge edge)
    {
        if (edge.output?.userData is not PortMetadata outputMeta || edge.input?.userData is not PortMetadata inputMeta)
        {
            return;
        }

        _window.RecordGraphChange("断开连线");

        switch (outputMeta.Link)
        {
            case LinkCategory.BuildingToLevel:
                DisconnectBuildingToLevel(outputMeta.Node, inputMeta.Node);
                break;
            case LinkCategory.LevelToRule:
                DisconnectLevelToRule(outputMeta.Node, inputMeta.Node);
                break;
            case LinkCategory.LevelToStatModifier:
                DisconnectLevelToModifier(outputMeta.Node, inputMeta.Node);
                break;
            case LinkCategory.RuleToCondition:
                DisconnectRuleToCondition(outputMeta.Node, inputMeta.Node);
                break;
            case LinkCategory.RuleToSuccessEffect:
                DisconnectRuleToEffect(outputMeta.Node, inputMeta.Node, true);
                break;
            case LinkCategory.RuleToFailureEffect:
                DisconnectRuleToEffect(outputMeta.Node, inputMeta.Node, false);
                break;
        }

        _window.GraphSerializedObject?.UpdateIfRequiredOrScript();
    }

    private void RemoveNodeData(GraphNodeView nodeView)
    {
        if (nodeView == null)
        {
            return;
        }

        _window.RecordGraphChange("删除节点");

        switch (nodeView.Category)
        {
            case NodeCategory.Level:
                RemoveLevelNode((LevelNodeView)nodeView);
                break;
            case NodeCategory.Rule:
                RemoveRuleNode((RuleNodeView)nodeView);
                break;
            case NodeCategory.Condition:
                RemoveConditionNode((ConditionNodeView)nodeView);
                break;
            case NodeCategory.EffectSuccess:
            case NodeCategory.EffectFailure:
                RemoveEffectNode((EffectNodeView)nodeView);
                break;
            case NodeCategory.StatModifier:
                RemoveStatModifierNode((StatModifierNodeView)nodeView);
                break;
        }

        _nodeLookup.Remove(nodeView.NodeId);
        _window.GraphSerializedObject?.UpdateIfRequiredOrScript();
    }

    private void RemoveLevelNode(LevelNodeView node)
    {
        var data = node.Data;
        _graphAsset.BuildingInfo.LevelNodeIds.Remove(data.Id);

        foreach (var ruleId in data.RuleNodeIds.ToList())
        {
            if (_nodeLookup.TryGetValue(ruleId, out var ruleNode))
            {
                DisconnectLevelToRule(node, ruleNode);
            }
        }

        foreach (var modifierId in data.StatModifierNodeIds.ToList())
        {
            if (_nodeLookup.TryGetValue(modifierId, out var modifierNode))
            {
                DisconnectLevelToModifier(node, modifierNode);
            }
        }

        _graphAsset.Levels.Remove(data);
    }

    private void RemoveRuleNode(RuleNodeView node)
    {
        var data = node.Data;
        foreach (var levelId in data.LevelNodeIds.ToList())
        {
            if (_nodeLookup.TryGetValue(levelId, out var levelNode))
            {
                DisconnectLevelToRule(levelNode, node);
            }
        }

        foreach (var conditionId in data.ConditionNodeIds.ToList())
        {
            if (_nodeLookup.TryGetValue(conditionId, out var conditionNode))
            {
                DisconnectRuleToCondition(node, conditionNode);
            }
        }

        foreach (var successId in data.SuccessEffectNodeIds.ToList())
        {
            if (_nodeLookup.TryGetValue(successId, out var effectNode))
            {
                DisconnectRuleToEffect(node, effectNode, true);
            }
        }

        foreach (var failureId in data.FailureEffectNodeIds.ToList())
        {
            if (_nodeLookup.TryGetValue(failureId, out var effectNode))
            {
                DisconnectRuleToEffect(node, effectNode, false);
            }
        }

        _graphAsset.Rules.Remove(data);
    }

    private void RemoveConditionNode(ConditionNodeView node)
    {
        var data = node.Data;
        foreach (var ruleId in data.RuleNodeIds.ToList())
        {
            if (_nodeLookup.TryGetValue(ruleId, out var ruleNode))
            {
                DisconnectRuleToCondition(ruleNode, node);
            }
        }

        _graphAsset.Conditions.Remove(data);
    }

    private void RemoveEffectNode(EffectNodeView node)
    {
        var data = node.Data;
        foreach (var ruleId in data.RuleNodeIds.ToList())
        {
            if (_nodeLookup.TryGetValue(ruleId, out var ruleNode))
            {
                bool isSuccess = data.Slot == EffectNodeData.EffectSlot.Success;
                DisconnectRuleToEffect(ruleNode, node, isSuccess);
            }
        }

        _graphAsset.Effects.Remove(data);
    }

    private void RemoveStatModifierNode(StatModifierNodeView node)
    {
        var data = node.Data;
        foreach (var levelId in data.LevelNodeIds.ToList())
        {
            if (_nodeLookup.TryGetValue(levelId, out var levelNode))
            {
                DisconnectLevelToModifier(levelNode, node);
            }
        }

        _graphAsset.StatModifiers.Remove(data);
    }

    private void ConnectBuildingToLevel(GraphNodeView buildingNode, GraphNodeView levelNode)
    {
        if (_graphAsset.BuildingInfo.LevelNodeIds.Contains(levelNode.NodeId))
        {
            return;
        }

        _graphAsset.BuildingInfo.LevelNodeIds.Add(levelNode.NodeId);
    }

    private void DisconnectBuildingToLevel(GraphNodeView buildingNode, GraphNodeView levelNode)
    {
        _graphAsset.BuildingInfo.LevelNodeIds.Remove(levelNode.NodeId);
    }

    private void ConnectLevelToRule(GraphNodeView levelNode, GraphNodeView ruleNode)
    {
        if (levelNode is not LevelNodeView levelView || ruleNode is not RuleNodeView ruleView)
        {
            return;
        }

        if (!levelView.Data.RuleNodeIds.Contains(ruleView.NodeId))
        {
            levelView.Data.RuleNodeIds.Add(ruleView.NodeId);
        }

        if (!ruleView.Data.LevelNodeIds.Contains(levelView.NodeId))
        {
            ruleView.Data.LevelNodeIds.Add(levelView.NodeId);
        }
    }

    private void DisconnectLevelToRule(GraphNodeView levelNode, GraphNodeView ruleNode)
    {
        if (levelNode is not LevelNodeView levelView || ruleNode is not RuleNodeView ruleView)
        {
            return;
        }

        levelView.Data.RuleNodeIds.Remove(ruleView.NodeId);
        ruleView.Data.LevelNodeIds.Remove(levelView.NodeId);
    }

    private void ConnectLevelToModifier(GraphNodeView levelNode, GraphNodeView modifierNode)
    {
        if (levelNode is not LevelNodeView levelView || modifierNode is not StatModifierNodeView modifierView)
        {
            return;
        }

        if (!levelView.Data.StatModifierNodeIds.Contains(modifierView.NodeId))
        {
            levelView.Data.StatModifierNodeIds.Add(modifierView.NodeId);
        }

        if (!modifierView.Data.LevelNodeIds.Contains(levelView.NodeId))
        {
            modifierView.Data.LevelNodeIds.Add(levelView.NodeId);
        }
    }

    private void DisconnectLevelToModifier(GraphNodeView levelNode, GraphNodeView modifierNode)
    {
        if (levelNode is not LevelNodeView levelView || modifierNode is not StatModifierNodeView modifierView)
        {
            return;
        }

        levelView.Data.StatModifierNodeIds.Remove(modifierView.NodeId);
        modifierView.Data.LevelNodeIds.Remove(levelView.NodeId);
    }

    private void ConnectRuleToCondition(GraphNodeView ruleNode, GraphNodeView conditionNode)
    {
        if (ruleNode is not RuleNodeView ruleView || conditionNode is not ConditionNodeView conditionView)
        {
            return;
        }

        if (!ruleView.Data.ConditionNodeIds.Contains(conditionView.NodeId))
        {
            ruleView.Data.ConditionNodeIds.Add(conditionView.NodeId);
        }

        if (!conditionView.Data.RuleNodeIds.Contains(ruleView.NodeId))
        {
            conditionView.Data.RuleNodeIds.Add(ruleView.NodeId);
        }
    }

    private void DisconnectRuleToCondition(GraphNodeView ruleNode, GraphNodeView conditionNode)
    {
        if (ruleNode is not RuleNodeView ruleView || conditionNode is not ConditionNodeView conditionView)
        {
            return;
        }

        ruleView.Data.ConditionNodeIds.Remove(conditionView.NodeId);
        conditionView.Data.RuleNodeIds.Remove(ruleView.NodeId);
    }

    private void ConnectRuleToEffect(GraphNodeView ruleNode, GraphNodeView effectNode, bool success)
    {
        if (ruleNode is not RuleNodeView ruleView || effectNode is not EffectNodeView effectView)
        {
            return;
        }

        var targetList = success ? ruleView.Data.SuccessEffectNodeIds : ruleView.Data.FailureEffectNodeIds;
        if (!targetList.Contains(effectView.NodeId))
        {
            targetList.Add(effectView.NodeId);
        }

        if (!effectView.Data.RuleNodeIds.Contains(ruleView.NodeId))
        {
            effectView.Data.RuleNodeIds.Add(ruleView.NodeId);
        }

        effectView.UpdateSlot(success);
    }

    private void DisconnectRuleToEffect(GraphNodeView ruleNode, GraphNodeView effectNode, bool success)
    {
        if (ruleNode is not RuleNodeView ruleView || effectNode is not EffectNodeView effectView)
        {
            return;
        }

        var targetList = success ? ruleView.Data.SuccessEffectNodeIds : ruleView.Data.FailureEffectNodeIds;
        targetList.Remove(effectView.NodeId);
        effectView.Data.RuleNodeIds.Remove(ruleView.NodeId);
    }

    public PortMetadata CreatePort(GraphNodeView nodeView, string portName, Direction direction, Port.Capacity capacity, LinkCategory category)
    {
        var port = nodeView.InstantiatePort(Orientation.Horizontal, direction, capacity, typeof(float));
        port.portName = portName;
        var metadata = new PortMetadata(nodeView, category);
        port.userData = metadata;
        if (direction == Direction.Input)
        {
            nodeView.inputContainer.Add(port);
        }
        else
        {
            nodeView.outputContainer.Add(port);
        }

        port.AddToClassList("port");
        return metadata;
    }
}

/// <summary>
/// 连线端口的附加信息。
/// </summary>
public class PortMetadata
{
    public GraphNodeView Node { get; }
    public LinkCategory Link { get; }

    public PortMetadata(GraphNodeView node, LinkCategory link)
    {
        Node = node;
        Link = link;
    }
}

/// <summary>
/// 基础节点视图。
/// </summary>
public abstract class GraphNodeView : Node
{
    protected readonly BuildingArchetypeGraphWindow Window;
    protected readonly GraphNodeData NodeData;

    public string NodeId => NodeData.Id;
    public string PropertyPath { get; }
    public NodeCategory Category { get; }

    protected GraphNodeView(BuildingArchetypeGraphWindow window, string propertyPath, GraphNodeData data, NodeCategory category)
    {
        Window = window;
        NodeData = data;
        PropertyPath = propertyPath;
        Category = category;

        viewDataKey = NodeId;
        style.width = 300f;
        style.flexShrink = 0f;

        RefreshTitle();
        RefreshExpandedState();
    }

    public void RefreshTitle()
    {
        var display = string.IsNullOrEmpty(NodeData.DisplayName) ? NodeData.GetType().Name : NodeData.DisplayName;
        title = display;
    }

    protected virtual void CustomizePropertyField(PropertyField propertyField)
    {
    }

    public virtual VisualElement CreateInspectorElement(SerializedObject serializedObject)
    {
        var container = new VisualElement();
        var property = serializedObject.FindProperty(PropertyPath);
        if (property == null)
        {
            container.Add(new Label("未找到序列化属性。"));
            return container;
        }

        var propertyField = new PropertyField(property)
        {
            label = title
        };
        propertyField.Bind(serializedObject);
        propertyField.RegisterValueChangeCallback(_ =>
        {
            Window.RecordGraphChange("编辑节点属性");
            RefreshTitle();
        });
        CustomizePropertyField(propertyField);
        container.Add(propertyField);
        return container;
    }

    public Port GetPort(LinkCategory category, Direction direction)
    {
        var ports = direction == Direction.Input ? inputContainer.Children() : outputContainer.Children();
        foreach (var child in ports)
        {
            if (child is Port port && port.userData is PortMetadata metadata && metadata.Link == category)
            {
                return port;
            }
        }

        return null;
    }
}

public class BuildingInfoNodeView : GraphNodeView
{
    public BuildingInfoNodeData Data { get; }

    public BuildingInfoNodeView(BuildingArchetypeGraphWindow window, string propertyPath, BuildingInfoNodeData data)
        : base(window, propertyPath, data, NodeCategory.BuildingInfo)
    {
        Data = data;

        var metadata = new PortMetadata(this, LinkCategory.BuildingToLevel);
        var port = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(float));
        port.portName = "等级";
        port.userData = metadata;
        outputContainer.Add(port);

        RefreshExpandedState();
        RefreshPorts();
    }
}

public class LevelNodeView : GraphNodeView
{
    public LevelNodeData Data { get; }

    public LevelNodeView(BuildingArchetypeGraphWindow window, string propertyPath, LevelNodeData data)
        : base(window, propertyPath, data, NodeCategory.Level)
    {
        Data = data;

        var buildingInput = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(float));
        buildingInput.portName = "建筑";
        buildingInput.userData = new PortMetadata(this, LinkCategory.BuildingToLevel);
        inputContainer.Add(buildingInput);

        var ruleOutput = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(float));
        ruleOutput.portName = "规则";
        ruleOutput.userData = new PortMetadata(this, LinkCategory.LevelToRule);
        outputContainer.Add(ruleOutput);

        var modifierOutput = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(float));
        modifierOutput.portName = "属性";
        modifierOutput.userData = new PortMetadata(this, LinkCategory.LevelToStatModifier);
        outputContainer.Add(modifierOutput);

        RefreshExpandedState();
        RefreshPorts();
    }
}

public class RuleNodeView : GraphNodeView
{
    public RuleNodeData Data { get; }
    private readonly Label _phaseLabel;

    public RuleNodeView(BuildingArchetypeGraphWindow window, string propertyPath, RuleNodeData data)
        : base(window, propertyPath, data, NodeCategory.Rule)
    {
        Data = data;

        var levelInput = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(float));
        levelInput.portName = "等级";
        levelInput.userData = new PortMetadata(this, LinkCategory.LevelToRule);
        inputContainer.Add(levelInput);

        var conditionOutput = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(float));
        conditionOutput.portName = "条件";
        conditionOutput.userData = new PortMetadata(this, LinkCategory.RuleToCondition);
        outputContainer.Add(conditionOutput);

        var successOutput = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(float));
        successOutput.portName = "成功";
        successOutput.userData = new PortMetadata(this, LinkCategory.RuleToSuccessEffect);
        outputContainer.Add(successOutput);

        var failureOutput = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(float));
        failureOutput.portName = "失败";
        failureOutput.userData = new PortMetadata(this, LinkCategory.RuleToFailureEffect);
        outputContainer.Add(failureOutput);

        _phaseLabel = new Label();
        mainContainer.Add(_phaseLabel);
        UpdatePhaseLabel();

        RefreshExpandedState();
        RefreshPorts();
    }

    protected override void CustomizePropertyField(PropertyField propertyField)
    {
        propertyField.RegisterValueChangeCallback(_ =>
        {
            UpdatePhaseLabel();
        });
    }

    private void UpdatePhaseLabel()
    {
        string phase = Data.RuleData != null ? Data.RuleData.Trigger.ToString() : "未知阶段";
        _phaseLabel.text = $"触发阶段：{phase}";
    }
}

public class ConditionNodeView : GraphNodeView
{
    public ConditionNodeData Data { get; }

    public ConditionNodeView(BuildingArchetypeGraphWindow window, string propertyPath, ConditionNodeData data)
        : base(window, propertyPath, data, NodeCategory.Condition)
    {
        Data = data;

        var ruleInput = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(float));
        ruleInput.portName = "规则";
        ruleInput.userData = new PortMetadata(this, LinkCategory.RuleToCondition);
        inputContainer.Add(ruleInput);

        RefreshExpandedState();
        RefreshPorts();
    }
}

public class EffectNodeView : GraphNodeView
{
    public EffectNodeData Data { get; }
    private readonly Port _inputPort;
    private readonly Label _slotTag;

    public EffectNodeView(BuildingArchetypeGraphWindow window, string propertyPath, EffectNodeData data)
        : base(window, propertyPath, data, data.Slot == EffectNodeData.EffectSlot.Success ? NodeCategory.EffectSuccess : NodeCategory.EffectFailure)
    {
        Data = data;

        _inputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(float));
        inputContainer.Add(_inputPort);

        _slotTag = new Label()
        {
            style =
            {
                unityFontStyleAndWeight = FontStyle.Bold,
                unityTextAlign = TextAnchor.MiddleCenter
            }
        };
        mainContainer.Add(_slotTag);

        RefreshSlotVisual();

        RefreshExpandedState();
        RefreshPorts();
    }

    protected override void CustomizePropertyField(PropertyField propertyField)
    {
        propertyField.RegisterValueChangeCallback(_ =>
        {
            RefreshSlotVisual();
        });
    }

    public void UpdateSlot(bool isSuccess)
    {
        Data.Slot = isSuccess ? EffectNodeData.EffectSlot.Success : EffectNodeData.EffectSlot.Failure;
        RefreshSlotVisual();
    }

    private void RefreshSlotVisual()
    {
        bool success = Data.Slot == EffectNodeData.EffectSlot.Success;
        _slotTag.text = success ? "成功效果" : "失败效果";
        _inputPort.portName = success ? "成功" : "失败";
        _inputPort.userData = new PortMetadata(this, success ? LinkCategory.RuleToSuccessEffect : LinkCategory.RuleToFailureEffect);
    }
}

public class StatModifierNodeView : GraphNodeView
{
    public StatModifierNodeData Data { get; }

    public StatModifierNodeView(BuildingArchetypeGraphWindow window, string propertyPath, StatModifierNodeData data)
        : base(window, propertyPath, data, NodeCategory.StatModifier)
    {
        Data = data;

        var levelInput = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(float));
        levelInput.portName = "等级";
        levelInput.userData = new PortMetadata(this, LinkCategory.LevelToStatModifier);
        inputContainer.Add(levelInput);

        RefreshExpandedState();
        RefreshPorts();
    }
}
