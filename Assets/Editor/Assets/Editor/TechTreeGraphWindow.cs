using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class TechTreeGraphWindow : EditorWindow
{
    private const string WindowTitle = "Tech Tree";

    private TechTreeAsset _currentAsset;
    private SerializedObject _serializedAsset;
    private TechTreeGraphView _graphView;
    private ScrollView _inspectorScroll;
    private IMGUIContainer _inspectorContainer;
    private ObjectField _assetField;
    private string _selectedNodePropertyPath = string.Empty;
    private TechNodeView _selectedNodeView;

    [MenuItem("LifeOn/Tech Tree Graph")]
    public static void Open()
    {
        TechTreeGraphWindow window = GetWindow<TechTreeGraphWindow>();
        window.titleContent = new GUIContent(WindowTitle);
        window.Show();
    }

    private void OnEnable()
    {
        Undo.undoRedoPerformed += OnUndoRedoPerformed;
        ConstructUI();
        if (_currentAsset == null && Selection.activeObject is TechTreeAsset asset)
        {
            LoadAsset(asset);
        }
    }

    private void OnDisable()
    {
        Undo.undoRedoPerformed -= OnUndoRedoPerformed;
    }

    private void ConstructUI()
    {
        rootVisualElement.Clear();

        Toolbar toolbar = new Toolbar();
        _assetField = new ObjectField("Tech Tree")
        {
            objectType = typeof(TechTreeAsset),
            allowSceneObjects = false,
            value = _currentAsset
        };
        _assetField.RegisterValueChangedCallback(evt =>
        {
            LoadAsset(evt.newValue as TechTreeAsset);
        });
        toolbar.Add(_assetField);

        toolbar.Add(new ToolbarButton(() => CreateAsset()) { text = "新建" });
        toolbar.Add(new ToolbarButton(() => SaveAsset()) { text = "保存" });
        toolbar.Add(new ToolbarButton(() => ReloadAsset()) { text = "重新载入" });
        toolbar.Add(new ToolbarButton(() => Undo.PerformUndo()) { text = "撤销" });
        toolbar.Add(new ToolbarButton(() => Undo.PerformRedo()) { text = "重做" });
        toolbar.Add(new ToolbarButton(() => CreateNodeAtCenter()) { text = "添加节点" });
        toolbar.Add(new ToolbarButton(() => ExportUnlockedIds()) { text = "导出ID" });
        toolbar.Add(new ToolbarButton(() => ValidateWithRuntime()) { text = "校验" });

        rootVisualElement.Add(toolbar);

        TwoPaneSplitView split = new TwoPaneSplitView(0, Mathf.Max(position.width * 0.65f, 320f), TwoPaneSplitViewOrientation.Horizontal);
        rootVisualElement.Add(split);

        _graphView = new TechTreeGraphView(this);
        _graphView.StretchToParentSize();
        split.Add(_graphView);

        _inspectorScroll = new ScrollView(ScrollViewMode.Vertical);
        _inspectorScroll.StretchToParentSize();
        _inspectorContainer = new IMGUIContainer(DrawInspector);
        _inspectorScroll.Add(_inspectorContainer);
        split.Add(_inspectorScroll);
    }

    private void DrawInspector()
    {
        if (_serializedAsset == null)
        {
            EditorGUILayout.LabelField("请选择或创建 TechTreeAsset");
            return;
        }

        _serializedAsset.Update();
        EditorGUILayout.LabelField("资产设置", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_serializedAsset.FindProperty("StartingNodeId"));
        EditorGUILayout.Space(8f);

        if (string.IsNullOrEmpty(_selectedNodePropertyPath))
        {
            EditorGUILayout.HelpBox("选择一个节点以编辑详细信息。", MessageType.Info);
        }
        else
        {
            SerializedProperty nodeProperty = _serializedAsset.FindProperty(_selectedNodePropertyPath);
            if (nodeProperty == null)
            {
                EditorGUILayout.HelpBox("节点序列化数据丢失。", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.LabelField("节点设置", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(nodeProperty.FindPropertyRelative("Id"));
                EditorGUILayout.PropertyField(nodeProperty.FindPropertyRelative("DisplayName"));
                EditorGUILayout.PropertyField(nodeProperty.FindPropertyRelative("Description"));
                EditorGUILayout.PropertyField(nodeProperty.FindPropertyRelative("Icon"));
                EditorGUILayout.PropertyField(nodeProperty.FindPropertyRelative("Prerequisites"), true);
                EditorGUILayout.PropertyField(nodeProperty.FindPropertyRelative("UnlockConditions"), true);
                EditorGUILayout.PropertyField(nodeProperty.FindPropertyRelative("OnUnlockEffects"), true);
                EditorGUI.indentLevel--;
            }
        }

        if (_serializedAsset.ApplyModifiedProperties())
        {
            if (_currentAsset != null)
            {
                EditorUtility.SetDirty(_currentAsset);
            }
            if (_selectedNodeView != null)
            {
                _selectedNodeView.Refresh();
            }
            _graphView?.RefreshAllNodeTitles();
        }
    }

    private void LoadAsset(TechTreeAsset asset)
    {
        _currentAsset = asset;
        _assetField.SetValueWithoutNotify(_currentAsset);
        _selectedNodePropertyPath = string.Empty;
        _selectedNodeView = null;

        if (_currentAsset != null)
        {
            _serializedAsset = new SerializedObject(_currentAsset);
        }
        else
        {
            _serializedAsset = null;
        }

        _graphView.Populate(_currentAsset);
    }

    private void CreateAsset()
    {
        string path = EditorUtility.SaveFilePanelInProject("创建 TechTreeAsset", "TechTreeAsset", "asset", "请选择保存位置");
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        TechTreeAsset asset = ScriptableObject.CreateInstance<TechTreeAsset>();
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        LoadAsset(asset);
    }

    private void SaveAsset()
    {
        if (_currentAsset == null)
        {
            return;
        }

        EditorUtility.SetDirty(_currentAsset);
        AssetDatabase.SaveAssets();
    }

    private void ReloadAsset()
    {
        if (_currentAsset == null)
        {
            return;
        }

        string path = AssetDatabase.GetAssetPath(_currentAsset);
        _currentAsset = AssetDatabase.LoadAssetAtPath<TechTreeAsset>(path);
        LoadAsset(_currentAsset);
    }

    private void CreateNodeAtCenter()
    {
        if (_graphView == null)
        {
            return;
        }

        Vector2 center = _graphView.contentViewContainer.WorldToLocal(new Vector2(position.width * 0.5f, position.height * 0.5f));
        CreateNode(center);
    }

    public void CreateNode(Vector2 position)
    {
        if (_currentAsset == null)
        {
            return;
        }

        Undo.RecordObject(_currentAsset, "Create Tech Node");
        TechNode node = new TechNode
        {
            Id = GenerateUniqueId("TechNode"),
            DisplayName = "新节点",
            EditorPosition = position
        };
        _currentAsset.Nodes.Add(node);
        EditorUtility.SetDirty(_currentAsset);
        _serializedAsset.Update();
        _graphView.AddNodeView(node);
    }

    private string GenerateUniqueId(string prefix)
    {
        HashSet<string> usedIds = new HashSet<string>(_currentAsset.Nodes.Select(n => n.Id));
        string id = prefix;
        int counter = 1;
        while (usedIds.Contains(id) || string.IsNullOrEmpty(id))
        {
            id = $"{prefix}_{counter}";
            counter++;
        }

        return id;
    }

    public void DeleteNode(TechNodeView nodeView)
    {
        if (_currentAsset == null || nodeView == null)
        {
            return;
        }

        Undo.RecordObject(_currentAsset, "Delete Tech Node");
        string removedId = nodeView.Node.Id;
        _currentAsset.Nodes.Remove(nodeView.Node);
        foreach (TechNode node in _currentAsset.Nodes)
        {
            node.Prerequisites.Remove(removedId);
        }
        EditorUtility.SetDirty(_currentAsset);
        _serializedAsset.Update();
        if (_selectedNodeView == nodeView)
        {
            _selectedNodeView = null;
            _selectedNodePropertyPath = string.Empty;
        }
        _inspectorContainer?.MarkDirtyRepaint();
    }

    public void AddLink(TechNodeView prerequisite, TechNodeView dependent)
    {
        if (_currentAsset == null || prerequisite == null || dependent == null)
        {
            return;
        }

        Undo.RecordObject(_currentAsset, "Add Tech Link");
        if (!dependent.Node.Prerequisites.Contains(prerequisite.Node.Id))
        {
            dependent.Node.Prerequisites.Add(prerequisite.Node.Id);
            EditorUtility.SetDirty(_currentAsset);
            _serializedAsset?.Update();
            _inspectorContainer?.MarkDirtyRepaint();
        }
    }

    public void RemoveLink(TechNodeView prerequisite, TechNodeView dependent)
    {
        if (_currentAsset == null || prerequisite == null || dependent == null)
        {
            return;
        }

        Undo.RecordObject(_currentAsset, "Remove Tech Link");
        if (dependent.Node.Prerequisites.Remove(prerequisite.Node.Id))
        {
            EditorUtility.SetDirty(_currentAsset);
            _serializedAsset?.Update();
            _inspectorContainer?.MarkDirtyRepaint();
        }
    }

    public void OnNodesMoved(IEnumerable<TechNodeView> nodeViews)
    {
        if (_currentAsset == null || nodeViews == null)
        {
            return;
        }

        List<TechNodeView> list = nodeViews.Where(view => view != null).ToList();
        if (list.Count == 0)
        {
            return;
        }

        Undo.RecordObject(_currentAsset, "Move Tech Node");
        foreach (TechNodeView nodeView in list)
        {
            Rect rect = nodeView.GetPosition();
            nodeView.Node.EditorPosition = rect.position;
        }

        EditorUtility.SetDirty(_currentAsset);
    }

    public void SelectNode(TechNodeView nodeView)
    {
        _selectedNodeView = nodeView;
        if (nodeView == null || _serializedAsset == null)
        {
            _selectedNodePropertyPath = string.Empty;
        }
        else
        {
            int index = _currentAsset.Nodes.IndexOf(nodeView.Node);
            if (index >= 0)
            {
                SerializedProperty nodesProperty = _serializedAsset.FindProperty("Nodes");
                SerializedProperty element = nodesProperty.GetArrayElementAtIndex(index);
                _selectedNodePropertyPath = element.propertyPath;
            }
            else
            {
                _selectedNodePropertyPath = string.Empty;
            }
        }

        _inspectorContainer?.MarkDirtyRepaint();
    }

    public bool IsSelected(TechNodeView nodeView)
    {
        return _selectedNodeView == nodeView;
    }

    private void ExportUnlockedIds()
    {
        if (_currentAsset == null)
        {
            EditorUtility.DisplayDialog(WindowTitle, "没有可导出的资产。", "确定");
            return;
        }

        string path = EditorUtility.SaveFilePanel("导出科技ID", Application.dataPath, "TechTreeIds", "txt");
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        TechTree tree = new TechTree();
        tree.LoadFromAsset(_currentAsset);
        List<string> ids = tree.GetAllNodes().Select(n => n.Id).Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();
        ids.Sort(StringComparer.Ordinal);
        File.WriteAllLines(path, ids);
        EditorUtility.RevealInFinder(path);
    }

    private void ValidateWithRuntime()
    {
        if (_currentAsset == null)
        {
            return;
        }

        TechTree tree = new TechTree();
        tree.LoadFromAsset(_currentAsset);
        int count = tree.GetAllNodes().Count();
        EditorUtility.DisplayDialog(WindowTitle, $"运行时校验完成，节点总数：{count}", "确定");
    }

    private void OnUndoRedoPerformed()
    {
        if (_currentAsset != null)
        {
            LoadAsset(_currentAsset);
            Repaint();
        }
    }
}

public class TechTreeGraphView : GraphView
{
    private readonly TechTreeGraphWindow _window;
    private readonly Dictionary<TechNode, TechNodeView> _nodeLookup = new Dictionary<TechNode, TechNodeView>();

    public TechTreeGraphView(TechTreeGraphWindow window)
    {
        _window = window;

        style.flexGrow = 1f;
        SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());

        GridBackground grid = new GridBackground();
        Insert(0, grid);
        grid.StretchToParentSize();

        graphViewChanged = OnGraphViewChanged;
    }

    public void Populate(TechTreeAsset asset)
    {
        foreach (Edge edge in edges.ToList())
        {
            RemoveElement(edge);
        }

        foreach (Node nodeElement in nodes.ToList())
        {
            RemoveElement(nodeElement);
        }

        _nodeLookup.Clear();

        if (asset == null || asset.Nodes == null)
        {
            return;
        }

        foreach (TechNode node in asset.Nodes)
        {
            AddNodeView(node);
        }

        foreach (TechNode node in asset.Nodes)
        {
            if (!_nodeLookup.TryGetValue(node, out TechNodeView nodeView))
            {
                continue;
            }

            foreach (string prerequisiteId in node.Prerequisites)
            {
                TechNodeView prerequisiteView = FindNodeViewById(prerequisiteId);
                if (prerequisiteView == null)
                {
                    continue;
                }

                Edge edge = prerequisiteView.OutputPort.ConnectTo(nodeView.InputPort);
                AddElement(edge);
            }
        }
    }

    public void AddNodeView(TechNode node)
    {
        TechNodeView nodeView = new TechNodeView(node, this, _window);
        _nodeLookup[node] = nodeView;
        AddElement(nodeView);
        nodeView.SetPosition(new Rect(node.EditorPosition, new Vector2(240f, 160f)));
    }

    public TechNodeView FindNodeViewById(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }

        foreach (KeyValuePair<TechNode, TechNodeView> pair in _nodeLookup)
        {
            if (pair.Key != null && pair.Key.Id == id)
            {
                return pair.Value;
            }
        }

        return null;
    }

    public void RefreshAllNodeTitles()
    {
        foreach (TechNodeView view in _nodeLookup.Values)
        {
            view.Refresh();
        }
    }

    public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
    {
        base.BuildContextualMenu(evt);
        Vector2 mousePosition = evt.localMousePosition;
        Vector2 graphPosition = ChangeCoordinatesTo(contentViewContainer, mousePosition);
        evt.menu.AppendAction("添加科技节点", action => _window.CreateNode(graphPosition));
    }

    private GraphViewChange OnGraphViewChanged(GraphViewChange change)
    {
        if (change.edgesToCreate != null)
        {
            foreach (Edge edge in change.edgesToCreate)
            {
                if (edge.output?.node is TechNodeView from && edge.input?.node is TechNodeView to)
                {
                    _window.AddLink(from, to);
                }
            }
        }

        if (change.elementsToRemove != null)
        {
            foreach (GraphElement element in change.elementsToRemove)
            {
                if (element is Edge edge)
                {
                    if (edge.output?.node is TechNodeView from && edge.input?.node is TechNodeView to)
                    {
                        _window.RemoveLink(from, to);
                    }
                }
                else if (element is TechNodeView nodeView)
                {
                    _window.DeleteNode(nodeView);
                    _nodeLookup.Remove(nodeView.Node);
                }
            }
        }

        if (change.movedElements != null)
        {
            List<TechNodeView> movedNodes = change.movedElements.OfType<TechNodeView>().ToList();
            if (movedNodes.Count > 0)
            {
                _window.OnNodesMoved(movedNodes);
            }
        }

        return change;
    }
}

public class TechNodeView : Node
{
    public TechNode Node { get; }
    public Port InputPort { get; }
    public Port OutputPort { get; }

    private readonly TechTreeGraphView _graphView;
    private readonly TechTreeGraphWindow _window;
    private readonly Image _iconImage;
    private readonly Label _descriptionLabel;

    public TechNodeView(TechNode node, TechTreeGraphView graphView, TechTreeGraphWindow window)
    {
        Node = node;
        _graphView = graphView;
        _window = window;

        viewDataKey = string.IsNullOrEmpty(node.Id) ? Guid.NewGuid().ToString() : node.Id;

        InputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(string));
        InputPort.portName = "前置";
        inputContainer.Add(InputPort);

        OutputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(string));
        OutputPort.portName = "解锁";
        outputContainer.Add(OutputPort);

        _iconImage = new Image
        {
            scaleMode = ScaleMode.ScaleToFit
        };
        _iconImage.style.width = 64f;
        _iconImage.style.height = 64f;
        _iconImage.style.alignSelf = Align.Center;
        mainContainer.Insert(0, _iconImage);

        _descriptionLabel = new Label();
        _descriptionLabel.style.whiteSpace = WhiteSpace.Normal;
        _descriptionLabel.style.unityTextAlign = TextAnchor.UpperLeft;
        extensionContainer.Add(_descriptionLabel);

        Refresh();
        RefreshExpandedState();
        RefreshPorts();
    }

    public void Refresh()
    {
        title = string.IsNullOrWhiteSpace(Node.DisplayName) ? Node.Id : Node.DisplayName;
        if (!string.IsNullOrEmpty(Node.Id))
        {
            viewDataKey = Node.Id;
        }
        if (Node.Icon != null)
        {
            _iconImage.image = Node.Icon.texture;
        }
        else
        {
            _iconImage.image = null;
        }

        _descriptionLabel.text = Node.Description;
    }

    public override void SetPosition(Rect newPos)
    {
        base.SetPosition(newPos);
        Node.EditorPosition = newPos.position;
    }

    public override void OnSelected()
    {
        base.OnSelected();
        _window.SelectNode(this);
    }

    public override void OnUnselected()
    {
        base.OnUnselected();
        if (_window != null && _window.IsSelected(this))
        {
            _window.SelectNode(null);
        }
    }
}
