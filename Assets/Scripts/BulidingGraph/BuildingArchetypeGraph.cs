using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// 通过图结构描述建筑原型数据，便于在 Inspector 中可视化编辑。
/// </summary>
[CreateAssetMenu(fileName = "BuildingArchetypeGraph", menuName = "Game/BuildingInstance/ArchetypeGraph")]
public class BuildingArchetypeGraph : ScriptableObject
{
    [SerializeField] private BuildingInfoNodeData _buildingInfo = new();
    [SerializeField] private List<LevelNodeData> _levels = new();
    [SerializeField] private List<RuleNodeData> _rules = new();
    [SerializeField] private List<ConditionNodeData> _conditions = new();
    [SerializeField] private List<EffectNodeData> _effects = new();
    [SerializeField] private List<StatModifierNodeData> _statModifiers = new();

    public BuildingInfoNodeData BuildingInfo => _buildingInfo;
    public IReadOnlyList<LevelNodeData> Levels => _levels;
    public IReadOnlyList<RuleNodeData> Rules => _rules;
    public IReadOnlyList<ConditionNodeData> Conditions => _conditions;
    public IReadOnlyList<EffectNodeData> Effects => _effects;
    public IReadOnlyList<StatModifierNodeData> StatModifiers => _statModifiers;

    /// <summary>
    /// 将图结构编译成 BuildingArchetype 资产。
    /// </summary>
    public void ToArchetype(BuildingArchetype target)
    {
        if (target == null)
        {
            ReportError("目标 BuildingArchetype 为空，无法写入数据。");
            return;
        }

        if (!ValidateGraph(out var errors))
        {
            ReportError(errors);
            return;
        }

        try
        {
            target.Id = _buildingInfo.BuildingId;
            target.DisplayName = _buildingInfo.InGameDisplayName;
            target.Size = _buildingInfo.Size;
            target.BuildingPrefab = _buildingInfo.Prefab;
            target.classification = _buildingInfo.Classification;

            target.Levels ??= new List<BuildingLevelDef>();
            target.Levels.Clear();

            var ruleLookup = _rules.ToDictionary(r => r.Id);
            var conditionLookup = _conditions.ToDictionary(c => c.Id);
            var effectLookup = _effects.ToDictionary(e => e.Id);
            var statLookup = _statModifiers.ToDictionary(s => s.Id);

            foreach (var levelId in _buildingInfo.LevelNodeIds)
            {
                if (!TryGetLevel(levelId, out var levelNode))
                {
                    continue;
                }

                BuildingLevelDef levelData = CloneLevel(levelNode.LevelData);
                levelData.Rules.Clear();
                levelData.ConditionalStatModifiers.Clear();

                foreach (var modifierId in levelNode.StatModifierNodeIds)
                {
                    if (!statLookup.TryGetValue(modifierId, out var modifierNode) || modifierNode.Modifier == null)
                    {
                        continue;
                    }

                    var modifierInstance = CloneReferenceObject(modifierNode.Modifier) as StatModifier;
                    levelData.ConditionalStatModifiers.Add(modifierInstance);
                }

                foreach (var ruleId in levelNode.RuleNodeIds)
                {
                    if (!ruleLookup.TryGetValue(ruleId, out var ruleNode) || ruleNode.RuleData == null)
                    {
                        continue;
                    }

                    Rule ruleInstance = CloneReferenceObject(ruleNode.RuleData) as Rule;
                    ruleInstance.Conditions.Clear();
                    ruleInstance.OnSuccess.Clear();
                    ruleInstance.OnFailure.Clear();

                    foreach (var conditionId in ruleNode.ConditionNodeIds)
                    {
                        if (!conditionLookup.TryGetValue(conditionId, out var conditionNode) || conditionNode.Condition == null)
                        {
                            continue;
                        }

                        var conditionInstance = CloneReferenceObject(conditionNode.Condition) as Condition;
                        ruleInstance.Conditions.Add(conditionInstance);
                    }

                    foreach (var successId in ruleNode.SuccessEffectNodeIds)
                    {
                        if (!effectLookup.TryGetValue(successId, out var effectNode) || effectNode.Effect == null)
                        {
                            continue;
                        }

                        if (effectNode.Slot != EffectNodeData.EffectSlot.Success)
                        {
                            Debug.LogWarning($"效果节点 {effectNode.DisplayName} 标记为 {effectNode.Slot}，但被编译为成功效果。", this);
                        }

                        var effectInstance = CloneReferenceObject(effectNode.Effect) as Effect;
                        ruleInstance.OnSuccess.Add(effectInstance);
                    }

                    foreach (var failureId in ruleNode.FailureEffectNodeIds)
                    {
                        if (!effectLookup.TryGetValue(failureId, out var effectNode) || effectNode.Effect == null)
                        {
                            continue;
                        }

                        if (effectNode.Slot != EffectNodeData.EffectSlot.Failure)
                        {
                            Debug.LogWarning($"效果节点 {effectNode.DisplayName} 标记为 {effectNode.Slot}，但被编译为失败效果。", this);
                        }

                        var effectInstance = CloneReferenceObject(effectNode.Effect) as Effect;
                        ruleInstance.OnFailure.Add(effectInstance);
                    }

                    levelData.Rules.Add(ruleInstance);
                }

                target.Levels.Add(levelData);
            }
        }
        catch (Exception ex)
        {
            ReportError($"编译 BuildingArchetypeGraph 失败：{ex.Message}\n{ex.StackTrace}");
        }
    }

    /// <summary>
    /// 从 BuildingArchetype 资产反解析为图结构，便于导入现有配置。
    /// </summary>
    public void FromArchetype(BuildingArchetype source)
    {
        _buildingInfo ??= new BuildingInfoNodeData();
        _buildingInfo.ForceSetIdIfEmpty();
        _buildingInfo.SetRuntimeType(typeof(BuildingArchetype));

        _buildingInfo.BuildingId = source != null ? source.Id : string.Empty;
        _buildingInfo.InGameDisplayName = source != null ? source.DisplayName : string.Empty;
        _buildingInfo.Size = source != null ? source.Size : 0;
        _buildingInfo.Prefab = source != null ? source.BuildingPrefab : null;
        _buildingInfo.Classification = source != null ? source.classification : BuildingClassify.基础;
        _buildingInfo.LevelNodeIds.Clear();

        _levels.Clear();
        _rules.Clear();
        _conditions.Clear();
        _effects.Clear();
        _statModifiers.Clear();

        if (source == null)
        {
            return;
        }

        foreach (var level in source.Levels)
        {
            var levelNode = new LevelNodeData();
            levelNode.ForceSetIdIfEmpty();
            levelNode.SetRuntimeType(typeof(BuildingLevelDef));
            levelNode.DisplayName = $"等级 {level.Level}";
            levelNode.LevelData = CloneLevel(level);
            levelNode.LevelData.Rules.Clear();
            levelNode.LevelData.ConditionalStatModifiers.Clear();
            levelNode.RuleNodeIds.Clear();
            levelNode.StatModifierNodeIds.Clear();

            foreach (var modifier in level.ConditionalStatModifiers)
            {
                var modifierNode = new StatModifierNodeData();
                modifierNode.ForceSetIdIfEmpty();
                modifierNode.DisplayName = modifier != null ? modifier.GetType().Name : "StatModifier";
                modifierNode.Modifier = modifier != null ? CloneReferenceObject(modifier) as StatModifier : null;
                modifierNode.SetRuntimeType(modifier?.GetType() ?? typeof(StatModifier));
                modifierNode.LevelNodeIds.Clear();
                modifierNode.LevelNodeIds.Add(levelNode.Id);

                _statModifiers.Add(modifierNode);
                levelNode.StatModifierNodeIds.Add(modifierNode.Id);
            }

            foreach (var rule in level.Rules)
            {
                var ruleNode = new RuleNodeData();
                ruleNode.ForceSetIdIfEmpty();
                ruleNode.SetRuntimeType(typeof(Rule));
                ruleNode.DisplayName = rule != null ? rule.Trigger.ToString() : "Rule";
                ruleNode.RuleData = rule != null ? CloneReferenceObject(rule) as Rule : new Rule();
                ruleNode.RuleData.Conditions.Clear();
                ruleNode.RuleData.OnSuccess.Clear();
                ruleNode.RuleData.OnFailure.Clear();
                ruleNode.ConditionNodeIds.Clear();
                ruleNode.SuccessEffectNodeIds.Clear();
                ruleNode.FailureEffectNodeIds.Clear();
                ruleNode.LevelNodeIds.Clear();
                ruleNode.LevelNodeIds.Add(levelNode.Id);

                foreach (var condition in rule.Conditions)
                {
                    var conditionNode = new ConditionNodeData();
                    conditionNode.ForceSetIdIfEmpty();
                    conditionNode.DisplayName = condition != null ? condition.GetType().Name : "Condition";
                    conditionNode.Condition = condition != null ? CloneReferenceObject(condition) as Condition : null;
                    conditionNode.SetRuntimeType(condition?.GetType() ?? typeof(Condition));
                    conditionNode.RuleNodeIds.Clear();
                    conditionNode.RuleNodeIds.Add(ruleNode.Id);

                    _conditions.Add(conditionNode);
                    ruleNode.ConditionNodeIds.Add(conditionNode.Id);
                }

                foreach (var success in rule.OnSuccess)
                {
                    var effectNode = new EffectNodeData();
                    effectNode.ForceSetIdIfEmpty();
                    effectNode.DisplayName = success != null ? success.GetType().Name : "Effect";
                    effectNode.Effect = success != null ? CloneReferenceObject(success) as Effect : null;
                    effectNode.Slot = EffectNodeData.EffectSlot.Success;
                    effectNode.SetRuntimeType(success?.GetType() ?? typeof(Effect));
                    effectNode.RuleNodeIds.Clear();
                    effectNode.RuleNodeIds.Add(ruleNode.Id);

                    _effects.Add(effectNode);
                    ruleNode.SuccessEffectNodeIds.Add(effectNode.Id);
                }

                foreach (var failure in rule.OnFailure)
                {
                    var effectNode = new EffectNodeData();
                    effectNode.ForceSetIdIfEmpty();
                    effectNode.DisplayName = failure != null ? failure.GetType().Name : "Effect";
                    effectNode.Effect = failure != null ? CloneReferenceObject(failure) as Effect : null;
                    effectNode.Slot = EffectNodeData.EffectSlot.Failure;
                    effectNode.SetRuntimeType(failure?.GetType() ?? typeof(Effect));
                    effectNode.RuleNodeIds.Clear();
                    effectNode.RuleNodeIds.Add(ruleNode.Id);

                    _effects.Add(effectNode);
                    ruleNode.FailureEffectNodeIds.Add(effectNode.Id);
                }

                _rules.Add(ruleNode);
                levelNode.RuleNodeIds.Add(ruleNode.Id);
            }

            _levels.Add(levelNode);
            _buildingInfo.LevelNodeIds.Add(levelNode.Id);
        }
    }

    private bool TryGetLevel(string id, out LevelNodeData node)
    {
        node = _levels.FirstOrDefault(l => l.Id == id);
        return node != null;
    }

    private bool ValidateGraph(out string errorMessage)
    {
        List<string> errors = new();
        HashSet<string> ids = new();

        if (_buildingInfo == null)
        {
            errors.Add("缺少建筑信息节点。");
        }
        else
        {
            _buildingInfo.ForceSetIdIfEmpty();
            _buildingInfo.SetRuntimeType(typeof(BuildingArchetype));
            if (!ids.Add(_buildingInfo.Id))
            {
                errors.Add($"建筑信息节点 ID 重复：{_buildingInfo.Id}");
            }
        }

        void ValidateNodeCollection<TNode>(IEnumerable<TNode> nodes, string category, Action<TNode> extra)
            where TNode : GraphNodeData
        {
            foreach (var node in nodes)
            {
                if (node == null)
                {
                    errors.Add($"{category} 中存在空节点。");
                    continue;
                }

                node.ForceSetIdIfEmpty();
                if (!ids.Add(node.Id))
                {
                    errors.Add($"{category} 节点 ID 重复：{node.Id}");
                }

                extra?.Invoke(node);
            }
        }

        ValidateNodeCollection(_levels, "等级", node =>
        {
            node.SetRuntimeType(typeof(BuildingLevelDef));
            if (node.LevelData == null)
            {
                errors.Add($"等级节点 {node.DisplayName} 缺少 LevelData。");
            }
        });

        ValidateNodeCollection(_rules, "规则", node =>
        {
            node.SetRuntimeType(typeof(Rule));
            if (node.RuleData == null)
            {
                errors.Add($"规则节点 {node.DisplayName} 缺少 RuleData。");
            }
        });

        ValidateNodeCollection(_conditions, "条件", node =>
        {
            if (node.Condition == null)
            {
                errors.Add($"条件节点 {node.DisplayName} 未指定 Condition 实例。");
            }
            else
            {
                node.SetRuntimeType(node.Condition.GetType());
            }
        });

        ValidateNodeCollection(_effects, "效果", node =>
        {
            if (node.Effect == null)
            {
                errors.Add($"效果节点 {node.DisplayName} 未指定 Effect 实例。");
            }
            else
            {
                node.SetRuntimeType(node.Effect.GetType());
            }
        });

        ValidateNodeCollection(_statModifiers, "属性修正", node =>
        {
            if (node.Modifier == null)
            {
                errors.Add($"属性修正节点 {node.DisplayName} 未指定 Modifier 实例。");
            }
            else
            {
                node.SetRuntimeType(node.Modifier.GetType());
            }
        });

        var allNodeIds = new HashSet<string>(_levels.Select(l => l.Id)
            .Concat(_rules.Select(r => r.Id))
            .Concat(_conditions.Select(c => c.Id))
            .Concat(_effects.Select(e => e.Id))
            .Concat(_statModifiers.Select(s => s.Id))
            .Concat(new[] { _buildingInfo?.Id ?? string.Empty }));

        // 引用校验
        foreach (var levelId in _buildingInfo.LevelNodeIds)
        {
            if (!allNodeIds.Contains(levelId))
            {
                errors.Add($"建筑信息节点引用了不存在的等级节点：{levelId}");
            }
        }

        foreach (var level in _levels)
        {
            foreach (var ruleId in level.RuleNodeIds)
            {
                if (!allNodeIds.Contains(ruleId))
                {
                    errors.Add($"等级节点 {level.DisplayName} 引用了不存在的规则节点：{ruleId}");
                }
            }

            foreach (var modifierId in level.StatModifierNodeIds)
            {
                if (!allNodeIds.Contains(modifierId))
                {
                    errors.Add($"等级节点 {level.DisplayName} 引用了不存在的属性修正节点：{modifierId}");
                }
            }
        }

        foreach (var rule in _rules)
        {
            foreach (var conditionId in rule.ConditionNodeIds)
            {
                if (!allNodeIds.Contains(conditionId))
                {
                    errors.Add($"规则节点 {rule.DisplayName} 引用了不存在的条件节点：{conditionId}");
                }
            }

            foreach (var successId in rule.SuccessEffectNodeIds)
            {
                if (!allNodeIds.Contains(successId))
                {
                    errors.Add($"规则节点 {rule.DisplayName} 引用了不存在的成功效果节点：{successId}");
                }
            }

            foreach (var failureId in rule.FailureEffectNodeIds)
            {
                if (!allNodeIds.Contains(failureId))
                {
                    errors.Add($"规则节点 {rule.DisplayName} 引用了不存在的失败效果节点：{failureId}");
                }
            }
        }

        foreach (var condition in _conditions)
        {
            foreach (var ruleId in condition.RuleNodeIds)
            {
                if (!allNodeIds.Contains(ruleId))
                {
                    errors.Add($"条件节点 {condition.DisplayName} 关联了不存在的规则节点：{ruleId}");
                }
            }
        }

        foreach (var effect in _effects)
        {
            foreach (var ruleId in effect.RuleNodeIds)
            {
                if (!allNodeIds.Contains(ruleId))
                {
                    errors.Add($"效果节点 {effect.DisplayName} 关联了不存在的规则节点：{ruleId}");
                }
            }
        }

        // 循环依赖检测（忽略回指引用）
        var adjacency = new Dictionary<string, List<string>>();
        void AddEdge(string from, string to, bool backReference = false)
        {
            if (backReference)
            {
                return;
            }

            if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to))
            {
                return;
            }

            if (!adjacency.TryGetValue(from, out var list))
            {
                list = new List<string>();
                adjacency[from] = list;
            }

            if (!list.Contains(to))
            {
                list.Add(to);
            }
        }

        if (_buildingInfo != null)
        {
            foreach (var levelId in _buildingInfo.LevelNodeIds)
            {
                AddEdge(_buildingInfo.Id, levelId);
            }
        }

        foreach (var level in _levels)
        {
            foreach (var ruleId in level.RuleNodeIds)
            {
                AddEdge(level.Id, ruleId);
            }

            foreach (var modifierId in level.StatModifierNodeIds)
            {
                AddEdge(level.Id, modifierId);
            }
        }

        foreach (var rule in _rules)
        {
            foreach (var conditionId in rule.ConditionNodeIds)
            {
                AddEdge(rule.Id, conditionId);
                AddEdge(conditionId, rule.Id, backReference: true);
            }

            foreach (var successId in rule.SuccessEffectNodeIds)
            {
                AddEdge(rule.Id, successId);
                AddEdge(successId, rule.Id, backReference: true);
            }

            foreach (var failureId in rule.FailureEffectNodeIds)
            {
                AddEdge(rule.Id, failureId);
                AddEdge(failureId, rule.Id, backReference: true);
            }
        }

        bool DetectCycle()
        {
            HashSet<string> visited = new();
            HashSet<string> stack = new();

            bool Dfs(string nodeId)
            {
                if (!visited.Add(nodeId))
                {
                    return false;
                }

                stack.Add(nodeId);
                if (adjacency.TryGetValue(nodeId, out var edges))
                {
                    foreach (var next in edges)
                    {
                        if (stack.Contains(next))
                        {
                            return true;
                        }

                        if (!visited.Contains(next) && Dfs(next))
                        {
                            return true;
                        }
                    }
                }

                stack.Remove(nodeId);
                return false;
            }

            foreach (var nodeId in adjacency.Keys)
            {
                if (!visited.Contains(nodeId) && Dfs(nodeId))
                {
                    return true;
                }
            }

            return false;
        }

        if (DetectCycle())
        {
            errors.Add("检测到图结构存在循环依赖，请检查节点引用是否形成闭环。");
        }

        errorMessage = string.Join("\n", errors.Distinct());
        return errors.Count == 0;
    }

    private static BuildingLevelDef CloneLevel(BuildingLevelDef source)
    {
        if (source == null)
        {
            return new BuildingLevelDef();
        }

        return CloneReferenceObject(source) as BuildingLevelDef;
    }

    private static object CloneReferenceObject(object source)
    {
        if (source == null)
        {
            return null;
        }

        var type = source.GetType();

        if (type.IsValueType || type == typeof(string) || typeof(UnityEngine.Object).IsAssignableFrom(type))
        {
            return source;
        }

        if (type.IsArray)
        {
            var elementType = type.GetElementType();
            if (elementType == null)
            {
                return null;
            }

            var array = (Array)source;
            var cloned = Array.CreateInstance(elementType, array.Length);
            for (int i = 0; i < array.Length; i++)
            {
                cloned.SetValue(CloneReferenceObject(array.GetValue(i)), i);
            }

            return cloned;
        }

        if (typeof(IList).IsAssignableFrom(type))
        {
            var list = (IList)Activator.CreateInstance(type);
            foreach (var item in (IEnumerable)source)
            {
                list.Add(CloneReferenceObject(item));
            }

            return list;
        }

        var instance = Activator.CreateInstance(type);
        CopyFieldsRecursive(source, instance, type);
        return instance;
    }

    private static void CopyFieldsRecursive(object source, object target, Type type)
    {
        if (type == null || type == typeof(object))
        {
            return;
        }

        CopyFieldsRecursive(source, target, type.BaseType);

        var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.DeclaredOnly;
        var fields = type.GetFields(flags);

        foreach (var field in fields)
        {
            if (field.IsInitOnly)
            {
                continue;
            }

            var value = field.GetValue(source);
            if (value == null)
            {
                field.SetValue(target, null);
                continue;
            }

            if (field.FieldType.IsValueType || field.FieldType == typeof(string) || typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType))
            {
                field.SetValue(target, value);
            }
            else
            {
                field.SetValue(target, CloneReferenceObject(value));
            }
        }
    }

    private void ReportError(string message)
    {
#if UNITY_EDITOR
        throw new UnityEditor.Build.BuildFailedException(message);
#else
        Debug.LogError(message, this);
#endif
    }

    private void OnValidate()
    {
        _buildingInfo ??= new BuildingInfoNodeData();
        _buildingInfo.ForceSetIdIfEmpty();
        _buildingInfo.SetRuntimeType(typeof(BuildingArchetype));

        foreach (var level in _levels)
        {
            level?.ForceSetIdIfEmpty();
            level?.SetRuntimeType(typeof(BuildingLevelDef));
        }

        foreach (var rule in _rules)
        {
            rule?.ForceSetIdIfEmpty();
            rule?.SetRuntimeType(typeof(Rule));
        }

        foreach (var condition in _conditions)
        {
            condition?.ForceSetIdIfEmpty();
            if (condition?.Condition != null)
            {
                condition.SetRuntimeType(condition.Condition.GetType());
            }
        }

        foreach (var effect in _effects)
        {
            effect?.ForceSetIdIfEmpty();
            if (effect?.Effect != null)
            {
                effect.SetRuntimeType(effect.Effect.GetType());
            }
        }

        foreach (var modifier in _statModifiers)
        {
            modifier?.ForceSetIdIfEmpty();
            if (modifier?.Modifier != null)
            {
                modifier.SetRuntimeType(modifier.Modifier.GetType());
            }
        }
    }

    #region Node Data Definitions

    [Serializable]
    public abstract class GraphNodeData
    {
        [LabelText("节点ID")]
        [SerializeField] private string _id;

        [LabelText("显示名")]
        [SerializeField] private string _displayName;

        [LabelText("运行时类型")]
        [ReadOnly]
        [SerializeField] private string _typeName;

        public string Id => _id;

        public string DisplayName
        {
            get => _displayName;
            set => _displayName = value;
        }

        public string TypeName => _typeName;

        public void ForceSetIdIfEmpty()
        {
            if (string.IsNullOrEmpty(_id))
            {
                _id = Guid.NewGuid().ToString("N");
            }
        }

        public void ForceSetId(string id)
        {
            _id = id;
        }

        public void SetRuntimeType(Type type)
        {
            _typeName = type != null ? type.AssemblyQualifiedName : string.Empty;
        }
    }

    [Serializable]
    public class BuildingInfoNodeData : GraphNodeData
    {
        [LabelText("建筑ID")]
        public string BuildingId;

        [LabelText("显示名（游戏内）")]
        public string InGameDisplayName;

        [LabelText("占地尺寸")]
        public int Size;

        [LabelText("建筑预制体")]
        public BuildingInstance Prefab;

        [LabelText("分类")]
        public BuildingClassify Classification = BuildingClassify.基础;

        [LabelText("等级节点引用")]
        public List<string> LevelNodeIds = new();
    }

    [Serializable]
    public class LevelNodeData : GraphNodeData
    {
        [LabelText("等级数据")]
        public BuildingLevelDef LevelData = new();

        [LabelText("规则节点引用")]
        public List<string> RuleNodeIds = new();

        [LabelText("属性修正节点引用")]
        public List<string> StatModifierNodeIds = new();
    }

    [Serializable]
    public class RuleNodeData : GraphNodeData
    {
        [LabelText("规则数据")]
        public Rule RuleData = new();

        [LabelText("条件节点引用")]
        public List<string> ConditionNodeIds = new();

        [LabelText("成功效果节点引用")]
        public List<string> SuccessEffectNodeIds = new();

        [LabelText("失败效果节点引用")]
        public List<string> FailureEffectNodeIds = new();

        [LabelText("所属等级")]
        public List<string> LevelNodeIds = new();
    }

    [Serializable]
    public class ConditionNodeData : GraphNodeData
    {
        [LabelText("条件对象")]
        [SerializeReference] public Condition Condition;

        [LabelText("关联规则")]
        public List<string> RuleNodeIds = new();
    }

    [Serializable]
    public class EffectNodeData : GraphNodeData
    {
        public enum EffectSlot
        {
            Success,
            Failure
        }

        [LabelText("效果对象")]
        [SerializeReference] public Effect Effect;

        [LabelText("效果类型")]
        public EffectSlot Slot = EffectSlot.Success;

        [LabelText("关联规则")]
        public List<string> RuleNodeIds = new();
    }

    [Serializable]
    public class StatModifierNodeData : GraphNodeData
    {
        [LabelText("修正对象")]
        [SerializeReference] public StatModifier Modifier;

        [LabelText("关联等级")]
        public List<string> LevelNodeIds = new();
    }

    #endregion
}