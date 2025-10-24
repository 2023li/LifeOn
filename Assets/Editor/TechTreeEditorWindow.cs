// Assets/Editor/TechTree/TechTreeEditorWindow.cs
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;

// 需要项目中已存在：TechTree.cs（定义 TechTree 与 TechNodeData）

public class TechTreeEditorWindow : EditorWindow
{
    [MenuItem("Window/LifeOn/Tech Tree Editor")]
    public static void OpenWindow()
    {
        var win = GetWindow<TechTreeEditorWindow>();
        win.titleContent = new GUIContent("Tech Tree Editor");
        win.Show();
    }

    private TechTree _currentTree;
    private TechTreeGraphView _graphView;
    private ObjectField _treeField;
    private bool _isDirty;

    private void MarkDirty()
    {
        _isDirty = true;
        if (titleContent != null) titleContent.text = "Tech Tree Editor *";
    }

    private void ClearDirty()
    {
        _isDirty = false;
        if (titleContent != null) titleContent.text = "Tech Tree Editor";
    }

    public void CreateGUI()
    {
        rootVisualElement.style.flexDirection = FlexDirection.Column;
        rootVisualElement.style.flexGrow = 1f;

        var toolbar = new Toolbar();

        _treeField = new ObjectField("TechTree Asset")
        {
            objectType = typeof(TechTree),
            allowSceneObjects = false,
            value = _currentTree
        };
        _treeField.RegisterValueChangedCallback(evt =>
        {
            var newTree = evt.newValue as TechTree;
            if (newTree != _currentTree)
            {
                _currentTree = newTree;
                RebuildGraphView();
            }
        });
        toolbar.Add(_treeField);

        var saveBtn = new ToolbarButton(() => SaveWithValidation()) { text = "保存 (Ctrl/Cmd + S)" };
        toolbar.Add(saveBtn);

        rootVisualElement.Add(toolbar);

        // 捕获 Ctrl/Cmd + S（UIElements）
        rootVisualElement.RegisterCallback<KeyDownEvent>(evt =>
        {
            if ((evt.ctrlKey || evt.commandKey) && evt.keyCode == KeyCode.S)
            {
                SaveWithValidation();
                evt.StopImmediatePropagation();
            }
        }, TrickleDown.TrickleDown);

        // 初始提示
        if (_currentTree == null)
        {
            var tip = new Label("请在上方选择一个 TechTree 资产进行编辑。")
            {
                style =
                {
                    flexGrow = 1f,
                    unityTextAlign = TextAnchor.MiddleCenter
                }
            };
            rootVisualElement.Add(tip);
        }

        Undo.undoRedoPerformed += OnUndoRedoPerformed;
    }

    private void OnDisable()
    {
        Undo.undoRedoPerformed -= OnUndoRedoPerformed;

        if (_currentTree != null && _isDirty)
        {
            bool ok = EditorUtility.DisplayDialog("保存更改？",
                "科技树有未保存的更改，是否保存？", "保存", "不保存");
            if (ok) SaveWithValidation();
        }
    }

    // 兜底捕获 Ctrl/Cmd + S（IMGUI）
    private void OnGUI()
    {
        var e = Event.current;
        if (e != null && e.type == EventType.KeyDown && (e.control || e.command) && e.keyCode == KeyCode.S)
        {
            SaveWithValidation();
            e.Use();
        }
    }

    private void OnUndoRedoPerformed()
    {
        _graphView?.ReloadFromTreeData();
        Repaint();
    }

    private void RebuildGraphView()
    {
        // 移除旧视图（不要调用 Dispose——GraphView 没这个方法）
        if (_graphView != null)
        {
            rootVisualElement.Remove(_graphView);
            _graphView = null;
        }

        // 清掉旧提示（Toolbar 在索引 0）
        for (int i = rootVisualElement.childCount - 1; i >= 1; i--)
            rootVisualElement.RemoveAt(i);

        if (_currentTree == null) return;

        _graphView = new TechTreeGraphView(_currentTree, MarkDirty);
        _graphView.StretchToParentSize();
        rootVisualElement.Add(_graphView);

        ClearDirty();
    }

    // 保存并校验重复ID
    private void SaveWithValidation()
    {
        if (_currentTree == null)
        {
            ShowNotification(new GUIContent("未选择 TechTree 资产"));
            return;
        }

        var dup = _currentTree.techList
            .GroupBy(t => t.id)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .OrderBy(x => x)
            .ToList();

        if (dup.Count > 0)
        {
            EditorUtility.DisplayDialog("保存失败：存在重复的科技ID",
                "以下 ID 出现重复：\n" + string.Join(", ", dup) + "\n\n请修正后再保存。", "好的");
            return;
        }

        EditorUtility.SetDirty(_currentTree);
        AssetDatabase.SaveAssets();
        ClearDirty();
        ShowNotification(new GUIContent("保存成功"));
    }

    //==================== GraphView ====================

    private class TechTreeGraphView : GraphView
    {
        private readonly TechTree _tree;
        private readonly Action _markDirty;

        internal readonly PortEdgeConnectorListener edgeConnectorListener;

        private readonly Dictionary<int, TechNodeView> _nodeViews = new Dictionary<int, TechNodeView>();
        private readonly Vector2 _defaultNodeSize = new Vector2(220, 180);

        public TechTreeGraphView(TechTree tree, Action markDirty)
        {
            _tree = tree;
            _markDirty = markDirty;

            // 交互：缩放、平移、框选、多选
            this.AddManipulator(new ContentZoomer());
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            // 网格背景
            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();

            // 监听变化
            graphViewChanged += OnGraphViewChanged;

            // 从端口拖拽到空白时新建节点
            edgeConnectorListener = new PortEdgeConnectorListener(this);

            // 空白处按空格创建节点（简单行为）
            nodeCreationRequest = ctx =>
            {
                Vector2 pos = contentViewContainer.WorldToLocal(ctx.screenMousePosition);
                CreateTechNodeAt(pos);
            };

            ReloadFromTreeData();
        }

        public void ReloadFromTreeData()
        {
            _nodeViews.Clear();
            DeleteElements(graphElements.ToList());

            // 节点
            foreach (var tech in _tree.techList)
            {
                var nv = new TechNodeView(tech, _tree, this);
                nv.SetPosition(new Rect(tech.position, _defaultNodeSize));
                AddElement(nv);
                _nodeViews[tech.id] = nv;
            }

            // 连线（依赖 -> 目标）
            foreach (var tech in _tree.techList)
            {
                if (tech.dependencies == null) continue;
                foreach (var depId in tech.dependencies)
                {
                    if (_nodeViews.TryGetValue(depId, out var from) &&
                        _nodeViews.TryGetValue(tech.id, out var to))
                    {
                        var edge = from.outputPort.ConnectTo(to.inputPort);
                        AddElement(edge);
                    }
                }
            }
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);
            if (evt.target is GraphView || evt.target is GridBackground)
            {
                evt.menu.AppendAction("添加新科技节点", _ =>
                {
                    Vector2 pos = contentViewContainer.WorldToLocal(evt.mousePosition);
                    CreateTechNodeAt(pos);
                });
                evt.menu.AppendAction("Frame All", _ => FrameAll());
            }
        }

        private void CreateTechNodeAt(Vector2 graphPos)
        {
            Undo.RecordObject(_tree, "Add Tech Node");

            var newTech = _tree.AddTech("新科技", "", 0, null);
            newTech.position = graphPos;

            var nv = new TechNodeView(newTech, _tree, this);
            nv.SetPosition(new Rect(graphPos, _defaultNodeSize));
            AddElement(nv);
            _nodeViews[newTech.id] = nv;

            SetDirty();
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            if (change.elementsToRemove != null)
            {
                foreach (var e in change.elementsToRemove)
                {
                    if (e is Edge edge)
                    {
                        var from = edge.output.node as TechNodeView;
                        var to = edge.input.node as TechNodeView;
                        if (from != null && to != null)
                        {
                            Undo.RecordObject(_tree, "Remove Dependency");
                            _tree.RemoveDependency(from.techData.id, to.techData.id);
                            SetDirty();
                        }
                    }

                    if (e is TechNodeView nv)
                    {
                        Undo.RecordObject(_tree, "Remove Tech Node");
                        int id = nv.techData.id;
                        _tree.RemoveTech(id);
                        _nodeViews.Remove(id);
                        SetDirty();
                    }
                }
            }

            if (change.edgesToCreate != null)
            {
                foreach (var edge in change.edgesToCreate)
                {
                    var from = edge.output.node as TechNodeView;
                    var to = edge.input.node as TechNodeView;
                    if (from != null && to != null)
                    {
                        Undo.RecordObject(_tree, "Add Dependency");
                        _tree.AddDependency(from.techData.id, to.techData.id);
                        SetDirty();
                    }
                }
            }

            return change;
        }

        internal void SetDirty()
        {
            _markDirty?.Invoke();
            EditorUtility.SetDirty(_tree);
        }

        // 端口拖拽监听：拖到空白自动建点并连接
        internal class PortEdgeConnectorListener : IEdgeConnectorListener
        {
            private readonly TechTreeGraphView _view;
            public PortEdgeConnectorListener(TechTreeGraphView view) { _view = view; }

            public void OnDropOutsidePort(Edge edge, Vector2 position)
            {
                Vector2 graphPos = _view.contentViewContainer.WorldToLocal(position);

                Undo.RecordObject(_view._tree, "Create Node By Drag");

                var newTech = _view._tree.AddTech("新科技", "", 0, null);
                newTech.position = graphPos;

                var newNode = new TechNodeView(newTech, _view._tree, _view);
                newNode.SetPosition(new Rect(graphPos, _view._defaultNodeSize));
                _view.AddElement(newNode);
                _view._nodeViews[newTech.id] = newNode;

                if (edge.output != null && edge.output.node is TechNodeView from)
                {
                    var newEdge = from.outputPort.ConnectTo(newNode.inputPort);
                    _view.AddElement(newEdge);
                    _view._tree.AddDependency(from.techData.id, newTech.id);
                }
                else if (edge.input != null && edge.input.node is TechNodeView to)
                {
                    var newEdge = newNode.outputPort.ConnectTo(to.inputPort);
                    _view.AddElement(newEdge);
                    _view._tree.AddDependency(newTech.id, to.techData.id);
                }

                _view.SetDirty();
            }

            public void OnDrop(GraphView graphView, Edge edge)
            {
                // 端口到端口：直接添加 Edge（GraphViewChanged 会把依赖写回数据）
                graphView.AddElement(edge);
            }
        }

        //==================== 节点视图 ====================
        private class TechNodeView : Node
        {
            public TechNodeData techData { get; private set; }
            public Port inputPort { get; private set; }
            public Port outputPort { get; private set; }

            private readonly TechTree _tree;
            private readonly TechTreeGraphView _owner;

            private Image _iconImage;

            public TechNodeView(TechNodeData data, TechTree tree, TechTreeGraphView owner)
            {
                techData = data;
                _tree = tree;
                _owner = owner;

                title = string.IsNullOrEmpty(data.name) ? $"Tech {data.id}" : data.name;
                tooltip = data.description;

                capabilities |= Capabilities.Movable | Capabilities.Deletable | Capabilities.Selectable;

                // 端口（使用默认 InstantiatePort，并正确挂 EdgeConnector<Edge>）
                inputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
                inputPort.portName = "";
                inputPort.AddManipulator(new EdgeConnector<Edge>(_owner.edgeConnectorListener));
                inputContainer.Add(inputPort);

                outputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(bool));
                outputPort.portName = "";
                outputPort.AddManipulator(new EdgeConnector<Edge>(_owner.edgeConnectorListener));
                outputContainer.Add(outputPort);

                // 标题图标
                _iconImage = new Image { style = { width = 18, height = 18 } };
                if (data.icon) _iconImage.image = data.icon.texture;
                titleContainer.Insert(0, _iconImage);

                // ===== 可编辑字段 =====
                var idField = new IntegerField("科技ID") { value = data.id };
                idField.RegisterValueChangedCallback(evt =>
                {
                    if (evt.newValue == techData.id) return;

                    Undo.RecordObject(_tree, "Edit Tech ID");

                    int oldId = techData.id;
                    int newId = evt.newValue;

                    // 替换依赖引用
                    foreach (var n in _tree.techList)
                    {
                        for (int i = 0; i < n.dependencies.Count; i++)
                            if (n.dependencies[i] == oldId) n.dependencies[i] = newId;
                    }

                    techData.id = newId;

                    // 更新映射
                    if (_owner._nodeViews.ContainsKey(oldId))
                    {
                        _owner._nodeViews.Remove(oldId);
                        _owner._nodeViews[newId] = this;
                    }

                    _owner.SetDirty();
                });
                extensionContainer.Add(idField);

                var nameField = new TextField("科技名称") { value = data.name };
                nameField.RegisterValueChangedCallback(evt =>
                {
                    Undo.RecordObject(_tree, "Edit Tech Name");
                    techData.name = evt.newValue;
                    title = string.IsNullOrEmpty(evt.newValue) ? $"Tech {techData.id}" : evt.newValue;
                    _owner.SetDirty();
                });
                extensionContainer.Add(nameField);

                var descField = new TextField("描述") { value = data.description, multiline = true };
                descField.style.minHeight = 60;
                descField.RegisterValueChangedCallback(evt =>
                {
                    Undo.RecordObject(_tree, "Edit Tech Description");
                    techData.description = evt.newValue;
                    tooltip = evt.newValue;
                    _owner.SetDirty();
                });
                extensionContainer.Add(descField);

                var iconField = new ObjectField("科技图标") { objectType = typeof(Sprite), value = data.icon };
                iconField.RegisterValueChangedCallback(evt =>
                {
                    Undo.RecordObject(_tree, "Edit Tech Icon");
                    techData.icon = evt.newValue as Sprite;
                    _iconImage.image = techData.icon ? techData.icon.texture : null;
                    _owner.SetDirty();
                });
                extensionContainer.Add(iconField);

                var costField = new IntegerField("需要的科研点") { value = data.cost };
                costField.RegisterValueChangedCallback(evt =>
                {
                    Undo.RecordObject(_tree, "Edit Tech Cost");
                    techData.cost = evt.newValue;
                    _owner.SetDirty();
                });
                extensionContainer.Add(costField);

                RefreshExpandedState();
                RefreshPorts();
            }

            public override void SetPosition(Rect newPos)
            {
                base.SetPosition(newPos);
                Undo.RecordObject(_tree, "Move Tech Node");
                techData.position = newPos.position;
                _owner.SetDirty();
            }
        }
    }
}
