using System.Collections.Generic;
using UnityEngine;

// 科技项数据结构定义
[System.Serializable]
public class TechNodeData
{
    public int id;                         // 科技ID
    public string name;                    // 科技名称
    public string description;             // 科技描述
    public Sprite icon;                    // 科技图标 (可为空)
    public int cost;                       // 所需科研点数
    public List<int> dependencies = new List<int>();  // 依赖的科技ID列表
    public Vector2 position;               // 节点在编辑器中的坐标，用于保存科技树布局
}

// 科技树类定义，继承自ScriptableObject以支持在Unity中序列化保存
[CreateAssetMenu(menuName = "LifeOn/TechTree", fileName = "TechTree")]
public class TechTree : ScriptableObject
{
    public List<TechNodeData> techList = new List<TechNodeData>();  // 所有科技节点的数据列表

    // 私有字典用于快速查找ID对应的科技数据（运行时使用，不会序列化）
    private Dictionary<int, TechNodeData> techDict = new Dictionary<int, TechNodeData>();

    // ScriptableObject加载时构建字典
    private void OnEnable()
    {
        BuildLookup();
    }

    // 重新构建ID查找字典
    private void BuildLookup()
    {
        techDict.Clear();
        foreach (TechNodeData tech in techList)
        {
            if (tech != null)
            {
                techDict[tech.id] = tech;
            }
        }
    }

    // 通过ID获取科技项数据，如果不存在返回null
    public TechNodeData GetTech(int id)
    {
        techDict.TryGetValue(id, out TechNodeData tech);
        return tech;
    }

    // 判断指定ID的科技是否存在
    public bool ContainsTech(int id)
    {
        return techDict.ContainsKey(id);
    }

    // 生成一个新的未使用的科技ID（简单实现：返回当前已用最大ID+1）
    public int GenerateNewId()
    {
        int newId = 1;
        foreach (var tech in techList)
        {
            if (tech.id >= newId)
                newId = tech.id + 1;
        }
        return newId;
    }

    // 新增一个科技节点，提供名称等参数（如果不提供则使用默认值），返回创建的TechNodeData
    public TechNodeData AddTech(string techName = "New Tech", string description = "", int cost = 0, Sprite icon = null)
    {
        // 生成唯一ID
        int newId = GenerateNewId();
        // 创建新科技数据
        TechNodeData newTech = new TechNodeData
        {
            id = newId,
            name = techName,
            description = description,
            cost = cost,
            icon = icon,
            dependencies = new List<int>(),
            position = Vector2.zero
        };
        // 添加到列表和字典
        techList.Add(newTech);
        techDict[newId] = newTech;
        return newTech;
    }

    // 删除指定ID的科技节点，返回是否删除成功
    public bool RemoveTech(int techId)
    {
        TechNodeData tech = GetTech(techId);
        if (tech == null) return false;
        // 从列表中移除
        techList.Remove(tech);
        techDict.Remove(techId);
        // 从其他科技的依赖列表中移除该ID
        foreach (TechNodeData t in techList)
        {
            if (t.dependencies != null && t.dependencies.Contains(techId))
            {
                t.dependencies.Remove(techId);
            }
        }
        return true;
    }

    // 为指定科技添加依赖（prereqId作为前置科技，techId为目标科技），返回是否添加成功
    public bool AddDependency(int prereqId, int techId)
    {
        TechNodeData tech = GetTech(techId);
        TechNodeData prereq = GetTech(prereqId);
        if (tech == null || prereq == null) return false;
        if (!tech.dependencies.Contains(prereqId) && tech.id != prereqId)
        {
            tech.dependencies.Add(prereqId);
            return true;
        }
        return false;
    }

    // 移除指定的依赖关系（prereqId不再是techId的前置科技），返回是否成功
    public bool RemoveDependency(int prereqId, int techId)
    {
        TechNodeData tech = GetTech(techId);
        if (tech != null && tech.dependencies.Contains(prereqId))
        {
            tech.dependencies.Remove(prereqId);
            return true;
        }
        return false;
    }

    // 判断某个科技的所有依赖是否都已解锁（unlockedIds为已解锁科技ID集合）
    public bool AreDependenciesMet(int techId, HashSet<int> unlockedIds)
    {
        TechNodeData tech = GetTech(techId);
        if (tech == null) return false;
        // 如果没有依赖，则直接可用；如果有依赖，则检查所有依赖ID是否都在已解锁集合中
        foreach (int depId in tech.dependencies)
        {
            if (!unlockedIds.Contains(depId))
                return false;
        }
        return true;
    }

    // 获取当前所有可研究（解锁）的科技列表：尚未解锁但依赖已全部满足的科技
    public List<TechNodeData> GetAvailableTechs(HashSet<int> unlockedIds)
    {
        List<TechNodeData> available = new List<TechNodeData>();
        foreach (TechNodeData tech in techList)
        {
            // 忽略已解锁的科技
            if (unlockedIds.Contains(tech.id))
                continue;
            // 如果科技无依赖或所有依赖都已解锁，则加入可用列表
            bool allDepsMet = true;
            foreach (int depId in tech.dependencies)
            {
                if (!unlockedIds.Contains(depId))
                {
                    allDepsMet = false;
                    break;
                }
            }
            if (allDepsMet)
            {
                available.Add(tech);
            }
        }
        return available;
    }
}
