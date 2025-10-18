#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class Build_UIPanel_DebugGridInspector_TMP : EditorWindow
{
    // ====== 配置字段（你手动赋值） ======
    [Header("Parent / Canvas")]
    public Transform ParentOverride = null;
    public bool CreateCanvasAndEventSystemIfMissing = true;

    [Header("Fonts (All TMP)")]
    public TMP_FontAsset TmpFont;                 // 必填：所有 TMP 文本/输入框使用的字体

    [Header("Binding (Optional)")]
    public bool TryBindToScriptNamedDebugGridInspector = true;
    public List<ScriptableObject> SupplyOptionsToBind = new();

    [Header("Save Prefab (Optional)")]
    public bool SaveAsPrefab = false;
    public string PrefabPath = "Assets/UIPanel_DebugGridInspector.prefab";

    private const int UI_LAYER = 5;

    // ====== 菜单入口 ======
    [MenuItem("Tools/UI/Build UIPanel_DebugGridInspector (TMP)")]
    public static void Open()
    {
        var window = GetWindow<Build_UIPanel_DebugGridInspector_TMP>("Build DebugGridInspector (TMP)");
        window.minSize = new Vector2(460, 480);
        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Parent / Canvas", EditorStyles.boldLabel);
        ParentOverride = (Transform)EditorGUILayout.ObjectField("Parent", ParentOverride, typeof(Transform), true);
        CreateCanvasAndEventSystemIfMissing = EditorGUILayout.Toggle("Auto Create Canvas/EventSystem", CreateCanvasAndEventSystemIfMissing);

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Fonts (All TMP)", EditorStyles.boldLabel);
        TmpFont = (TMP_FontAsset)EditorGUILayout.ObjectField("TMP Font Asset", TmpFont, typeof(TMP_FontAsset), false);
        if (TmpFont == null)
            EditorGUILayout.HelpBox("请指定 TMP_FontAsset（必填）。", MessageType.Warning);

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Binding (Optional)", EditorStyles.boldLabel);
        TryBindToScriptNamedDebugGridInspector = EditorGUILayout.Toggle("Try Bind DebugGridInspector", TryBindToScriptNamedDebugGridInspector);

        int newCount = Mathf.Max(0, EditorGUILayout.IntField("SupplyOptions Count", SupplyOptionsToBind.Count));
        while (newCount > SupplyOptionsToBind.Count) SupplyOptionsToBind.Add(null);
        while (newCount < SupplyOptionsToBind.Count) SupplyOptionsToBind.RemoveAt(SupplyOptionsToBind.Count - 1);
        for (int i = 0; i < SupplyOptionsToBind.Count; i++)
        {
            SupplyOptionsToBind[i] = (ScriptableObject)EditorGUILayout.ObjectField($"  [{i}]", SupplyOptionsToBind[i], typeof(ScriptableObject), false);
        }

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Save Prefab (Optional)", EditorStyles.boldLabel);
        SaveAsPrefab = EditorGUILayout.Toggle("Save As Prefab", SaveAsPrefab);
        using (new EditorGUI.DisabledScope(!SaveAsPrefab))
        {
            EditorGUILayout.BeginHorizontal();
            PrefabPath = EditorGUILayout.TextField("Prefab Path", PrefabPath);
            if (GUILayout.Button("...", GUILayout.Width(28)))
            {
                var dir = string.IsNullOrEmpty(PrefabPath) ? "Assets" : System.IO.Path.GetDirectoryName(PrefabPath);
                var file = string.IsNullOrEmpty(PrefabPath) ? "UIPanel_DebugGridInspector.prefab" : System.IO.Path.GetFileName(PrefabPath);
                var savePath = EditorUtility.SaveFilePanelInProject("Save Prefab", file, "prefab", "Choose prefab save path", dir);
                if (!string.IsNullOrEmpty(savePath)) PrefabPath = savePath;
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(12);
        using (new EditorGUI.DisabledScope(TmpFont == null))
        {
            if (GUILayout.Button("Build (All TMP)", GUILayout.Height(38)))
            {
                Build();
            }
        }
    }

    private void Build()
    {
        if (TmpFont == null)
        {
            Debug.LogError("TMP_FontAsset 未指定。");
            return;
        }

        // 1) Canvas & EventSystem
        var parent = ParentOverride != null ? ParentOverride : EnsureCanvasAndEventSystem();

        // 2) 根面板
        var rootRT = CreateUI("UIPanel_DebugGridInspector", parent, out var rootGO);
        SetLayerRecursively(rootGO, UI_LAYER);
        StretchFull(rootRT);

        var rootImg = rootGO.AddComponent<Image>();
        rootImg.color = new Color(0f, 0f, 0f, 0.25f);

        // 3) 顶部三段文本（全部 TMP）
        var coordinateText = CreateTMPLabel(rootRT, "CoordinateText", "坐标: -", new Vector2(40, -40), new Vector2(520, 40), 32);
        var buildingNameText = CreateTMPLabel(rootRT, "BuildingNameText", "建筑: -", new Vector2(40, -90), new Vector2(520, 40), 32);
        var storageInfoText = CreateTMPLabel(rootRT, "StorageInfoText", "仓库: -", new Vector2(40, -140), new Vector2(520, 200), 28, wordWrap: true);

        // 4) StorageControls（默认隐藏）
        var storageControlsRoot = CreateUI("StorageControls", rootRT, out var controlsGO);
        AnchorTopLeft(storageControlsRoot, new Vector2(40, -360), new Vector2(800, 200));
        controlsGO.SetActive(false);

        // 4.A) SupplyDropdownButton + Label（TMP）
        var dropdownBtnRT = CreateUI("SupplyDropdownButton", storageControlsRoot, out var dropdownBtnGO);
        AnchorTopLeft(dropdownBtnRT, Vector2.zero, new Vector2(220, 50));
        var dropdownBtnImg = dropdownBtnGO.AddComponent<Image>();
        dropdownBtnImg.color = new Color(1f, 1f, 1f, 0.5f);
        var supplyDropdownButton = dropdownBtnGO.AddComponent<Button>();
        var supplyDropdownLabel = CreateTMPLabelFill(dropdownBtnRT, "Label", "选择物资", 28, innerPadding: 10, raycastTarget: false);

        // 4.B) SupplyDropdownList（TMP 容器）
        var supplyDropdownListRoot = CreateUI("SupplyDropdownList", storageControlsRoot, out var dropdownListGO);
        AnchorTopLeft(supplyDropdownListRoot, new Vector2(0, -55), new Vector2(220, 0));
        var dropdownListImg = dropdownListGO.AddComponent<Image>();
        dropdownListImg.color = new Color(1f, 1f, 1f, 0.314f);
        dropdownListGO.SetActive(false);

        // └─ OptionTemplate（按钮模板，默认隐藏）
        var optionTemplateRT = CreateUI("OptionTemplate", supplyDropdownListRoot, out var optionTemplateGO);
        AnchorTopLeft(optionTemplateRT, Vector2.zero, new Vector2(220, 50));
        var optionTemplateImg = optionTemplateGO.AddComponent<Image>();
        optionTemplateImg.color = new Color(1f, 1f, 1f, 0.5f);
        var supplyOptionButtonPrefab = optionTemplateGO.AddComponent<Button>();
        CreateTMPLabelFill(optionTemplateRT, "Label", "选项", 26, innerPadding: 10, raycastTarget: false);
        optionTemplateGO.SetActive(false);

        // 4.C) AmountInput（改为 TMP_InputField）
        var amountRT = CreateUI("AmountInput", storageControlsRoot, out var amountGO);
        AnchorTopLeft(amountRT, new Vector2(240, 0), new Vector2(160, 50));
        var amountBg = amountGO.AddComponent<Image>();
        amountBg.color = new Color(1f, 1f, 1f, 0.5f);
        TMP_InputField supplyAmountInput = CreateTMPInputField(amountRT, placeholderText: "输入数量", fontSize: 26);

        // 4.D) AddButton + Label（TMP）
        var addBtnRT = CreateUI("AddButton", storageControlsRoot, out var addBtnGO);
        AnchorTopLeft(addBtnRT, new Vector2(420, 0), new Vector2(160, 50));
        var addBtnImg = addBtnGO.AddComponent<Image>();
        addBtnImg.color = new Color(0.3529412f, 0.6901961f, 0.39215687f, 1f);
        var addSupplyButton = addBtnGO.AddComponent<Button>();
        CreateTMPLabelFill(addBtnRT, "Label", "添加", 28, innerPadding: 10, raycastTarget: false);

        // 5) 反射绑定（可选）
        if (TryBindToScriptNamedDebugGridInspector)
        {
            TryBindDebugGridInspector(
                rootGO,
                coordinateText,
                buildingNameText,
                storageInfoText,
                storageControlsRoot,
                supplyDropdownButton,
                supplyDropdownLabel,
                supplyDropdownListRoot,
                supplyOptionButtonPrefab,
                supplyAmountInput,
                addSupplyButton,
                SupplyOptionsToBind
            );
        }

        // 6) 保存 Prefab（可选）
        if (SaveAsPrefab)
        {
            SavePrefab(rootGO, PrefabPath);
            Debug.Log($"Saved prefab to: {PrefabPath}");
        }

        // 选中结果
        Selection.activeObject = rootGO;
        EditorGUIUtility.PingObject(rootGO);
    }

    // ====================== 内部实现 ======================

    private Transform EnsureCanvasAndEventSystem()
    {
        Canvas canvas = null;

        var canvases = GameObject.FindObjectsOfType<Canvas>();
        canvas = canvases.FirstOrDefault(c => c.isActiveAndEnabled && c.renderMode != RenderMode.WorldSpace);
        if (canvas == null && CreateCanvasAndEventSystemIfMissing)
        {
            var go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            go.layer = UI_LAYER;
        }

        if (CreateCanvasAndEventSystemIfMissing && GameObject.FindObjectOfType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            es.layer = UI_LAYER;
        }
        return canvas != null ? canvas.transform : null;
    }

    private static RectTransform CreateUI(string name, Transform parent, out GameObject go)
    {
        go = new GameObject(name, typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        go.layer = UI_LAYER;
        rt.SetParent(parent, false);
        rt.localScale = Vector3.one;
        rt.localPosition = Vector3.zero;
        rt.localRotation = Quaternion.identity;
        return rt;
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
    }

    private static void AnchorTopLeft(RectTransform rt, Vector2 anchoredPos, Vector2 size)
    {
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
    }

    // ====== TMP Label（重载 1：锚点 + 尺寸）======
    private TextMeshProUGUI CreateTMPLabel(
        RectTransform parent,
        string name,
        string text,
        Vector2 anchorPos,
        Vector2 size,
        int fontSize,
        bool wordWrap = false,
        bool raycastTarget = true)
    {
        var rt = CreateUI(name, parent, out var go);
        AnchorTopLeft(rt, anchorPos, size);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.enableWordWrapping = wordWrap;
        tmp.raycastTarget = raycastTarget;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Left | TextAlignmentOptions.Midline;
        if (TmpFont != null) tmp.font = TmpFont;
        return tmp;
    }

    // ====== TMP Label（重载 2：填满父节点）======
    private TextMeshProUGUI CreateTMPLabelFill(
        RectTransform parent,
        string name,
        string text,
        int fontSize,
        float innerPadding = 0f,
        bool wordWrap = false,
        bool raycastTarget = true)
    {
        var rt = CreateUI(name, parent, out var go);
        // Fill parent with padding
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(innerPadding, innerPadding);
        rt.offsetMax = new Vector2(-innerPadding, -innerPadding);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.enableWordWrapping = wordWrap;
        tmp.raycastTarget = raycastTarget;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Left | TextAlignmentOptions.Midline;
        if (TmpFont != null) tmp.font = TmpFont;
        return tmp;
    }

    // ====== TMP_InputField 构建 ======
    private TMP_InputField CreateTMPInputField(RectTransform parent, string placeholderText, int fontSize)
    {
        // 容器已经存在（parent），直接创建子对象
        // Text Area
        var textAreaRT = CreateUI("Text Area", parent, out var textAreaGO);
        textAreaRT.anchorMin = Vector2.zero;
        textAreaRT.anchorMax = Vector2.one;
        textAreaRT.pivot = new Vector2(0.5f, 0.5f);
        textAreaRT.offsetMin = new Vector2(10, 10);
        textAreaRT.offsetMax = new Vector2(-10, -10);

        // Placeholder (TMP)
        var placeholderRT = CreateUI("Placeholder", textAreaRT, out var placeholderGO);
        placeholderRT.anchorMin = Vector2.zero;
        placeholderRT.anchorMax = Vector2.one;
        placeholderRT.pivot = new Vector2(0.5f, 0.5f);
        placeholderRT.offsetMin = Vector2.zero;
        placeholderRT.offsetMax = Vector2.zero;
        var placeholderTMP = placeholderGO.AddComponent<TextMeshProUGUI>();
        placeholderTMP.text = placeholderText;
        placeholderTMP.fontSize = fontSize - 2;
        placeholderTMP.fontStyle = FontStyles.Italic;
        placeholderTMP.color = new Color(0.78f, 0.78f, 0.78f, 0.8f);
        placeholderTMP.alignment = TextAlignmentOptions.MidlineLeft;
        if (TmpFont != null) placeholderTMP.font = TmpFont;
        placeholderTMP.raycastTarget = false;

        // Text (TMP)
        var textRT = CreateUI("Text", textAreaRT, out var textGO);
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.pivot = new Vector2(0.5f, 0.5f);
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;
        var textTMP = textGO.AddComponent<TextMeshProUGUI>();
        textTMP.text = "";
        textTMP.fontSize = fontSize;
        textTMP.alignment = TextAlignmentOptions.MidlineLeft;
        textTMP.color = new Color(0.196f, 0.196f, 0.196f, 1f);
        textTMP.enableWordWrapping = false;
        if (TmpFont != null) textTMP.font = TmpFont;

        // TMP_InputField
        TMP_InputField inputField = parent.gameObject.AddComponent<TMP_InputField>();
        inputField.textViewport = textAreaRT;
        inputField.pointSize = fontSize;
        inputField.textComponent = textTMP;
        inputField.placeholder = placeholderTMP;
        inputField.caretBlinkRate = 0.85f;
        inputField.caretWidth = 1;
        inputField.interactable = true;
        inputField.richText = true;
        inputField.contentType = TMP_InputField.ContentType.Standard;
        inputField.lineType = TMP_InputField.LineType.SingleLine;
        inputField.characterValidation = TMP_InputField.CharacterValidation.None;

        return inputField;
    }

    // ====== 反射绑定 DebugGridInspector ======
    private static IEnumerable<Type> SafeGetTypes(Assembly a)
    {
        try { return a.GetTypes(); }
        catch { return Array.Empty<Type>(); }
    }

    private static FieldInfo GetField(object target, string name)
    {
        if (target == null) return null;
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        return target.GetType().GetField(name, flags);
    }

    private static void SetFieldIfExists(object target, string fieldName, object value)
    {
        var f = GetField(target, fieldName);
        if (f != null && (value == null || f.FieldType.IsAssignableFrom(value.GetType())))
        {
            f.SetValue(target, value);
        }
    }

    private static void TryBindDebugGridInspector(
        GameObject root,
        TextMeshProUGUI coordinateText,
        TextMeshProUGUI buildingNameText,
        TextMeshProUGUI storageInfoText,
        RectTransform storageControlsRoot,
        Button supplyDropdownButton,
        TextMeshProUGUI supplyDropdownLabel,
        RectTransform supplyDropdownListRoot,
        Button supplyOptionButtonPrefab,
        TMP_InputField supplyAmountInput,
        Button addSupplyButton,
        List<ScriptableObject> supplyOptionsToBind)
    {
        var t = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => SafeGetTypes(a))
            .FirstOrDefault(x => x != null && x.Name == "DebugGridInspector");

        if (t == null) return;

        var comp = root.GetComponent(t) ?? root.AddComponent(t);

        SetFieldIfExists(comp, "_coordinateText", coordinateText);
        SetFieldIfExists(comp, "_buildingNameText", buildingNameText);
        SetFieldIfExists(comp, "_storageInfoText", storageInfoText);
        SetFieldIfExists(comp, "_storageControlsRoot", storageControlsRoot);
        SetFieldIfExists(comp, "_supplyDropdownButton", supplyDropdownButton);
        SetFieldIfExists(comp, "_supplyDropdownLabel", supplyDropdownLabel);
        SetFieldIfExists(comp, "_supplyDropdownListRoot", supplyDropdownListRoot);
        SetFieldIfExists(comp, "_supplyOptionButtonPrefab", supplyOptionButtonPrefab);
        SetFieldIfExists(comp, "_supplyAmountInput", supplyAmountInput);
        SetFieldIfExists(comp, "_addSupplyButton", addSupplyButton);

        if (supplyOptionsToBind != null && supplyOptionsToBind.Count > 0)
        {
            var listField = GetField(comp, "SupplyOptions");
            if (listField != null)
            {
                var fType = listField.FieldType;
                if (typeof(IList<ScriptableObject>).IsAssignableFrom(fType))
                {
                    listField.SetValue(comp, supplyOptionsToBind);
                }
                else if (fType.IsArray && fType.GetElementType().IsAssignableFrom(typeof(ScriptableObject)))
                {
                    listField.SetValue(comp, supplyOptionsToBind.ToArray());
                }
            }
        }
    }

    // ====== 保存 Prefab ======
    private static void SavePrefab(GameObject root, string path)
    {
        var dir = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        if (!AssetDatabase.IsValidFolder(dir))
        {
            // 迭代创建目录
            var parts = dir.Split('/');
            string cur = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                var next = $"{cur}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(cur, parts[i]);
                cur = next;
            }
        }

        PrefabUtility.SaveAsPrefabAssetAndConnect(root, path, InteractionMode.UserAction);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void SetLayerRecursively(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
            SetLayerRecursively(child.gameObject, layer);
    }
}
#endif
