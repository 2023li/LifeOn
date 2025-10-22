using System.IO;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BuildingArchetype))]
public class BuildingArchetypeEditor : Editor
{
    private SerializedProperty _graphProperty;

    private void OnEnable()
    {
        _graphProperty = serializedObject.FindProperty("GraphAsset");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawPropertiesExcluding(serializedObject, "m_Script", "GraphAsset");

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.ObjectField("Graph Asset", _graphProperty?.objectReferenceValue, typeof(BuildingArchetypeGraph), false);
        }

        EditorGUILayout.Space();

        using (new EditorGUI.DisabledScope(targets.Length != 1))
        {
            if (GUILayout.Button("打开节点编辑器"))
            {
                OpenGraphEditor();
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void OpenGraphEditor()
    {
        var archetype = target as BuildingArchetype;
        if (archetype == null)
        {
            return;
        }

        serializedObject.Update();
        var graph = _graphProperty?.objectReferenceValue as BuildingArchetypeGraph;

        if (graph == null)
        {
            if (!EditorUtility.DisplayDialog("缺少图资产", "当前建筑原型尚未绑定图资产，是否基于现有数据创建一个新的图？", "创建", "取消"))
            {
                return;
            }

            string archetypePath = AssetDatabase.GetAssetPath(archetype);
            string defaultFolder = string.IsNullOrEmpty(archetypePath) ? "Assets" : Path.GetDirectoryName(archetypePath);
            string defaultName = string.IsNullOrEmpty(archetype.name) ? "BuildingArchetypeGraph" : $"{archetype.name}_Graph";
            string graphPath = EditorUtility.SaveFilePanelInProject("创建 Building Archetype Graph", defaultName, "asset", "请选择图资产的保存路径", defaultFolder);
            if (string.IsNullOrEmpty(graphPath))
            {
                return;
            }

            graph = ScriptableObject.CreateInstance<BuildingArchetypeGraph>();
            Undo.RegisterCreatedObjectUndo(graph, "Create Building Archetype Graph");
            graph.SetLinkedArchetype(archetype);
            graph.FromArchetype(archetype);
            AssetDatabase.CreateAsset(graph, graphPath);
            AssetDatabase.SaveAssets();
            EditorUtility.SetDirty(graph);

            _graphProperty.objectReferenceValue = graph;
            serializedObject.ApplyModifiedProperties();
        }

        EnsureLink(archetype, graph);

        bool autoImport = graph != null && graph.IsEmpty();
        BuildingArchetypeGraphWindow.OpenWithAssets(graph, archetype, autoImport);
    }

    private static void EnsureLink(BuildingArchetype archetype, BuildingArchetypeGraph graph)
    {
        if (archetype == null || graph == null)
        {
            return;
        }

        if (archetype.GraphAsset != graph)
        {
            Undo.RecordObject(archetype, "Assign Graph Asset");
            archetype.GraphAsset = graph;
            EditorUtility.SetDirty(archetype);
        }

        if (graph.LinkedArchetype != archetype)
        {
            Undo.RecordObject(graph, "Assign Linked Archetype");
            graph.SetLinkedArchetype(archetype);
            EditorUtility.SetDirty(graph);
        }
    }
}





