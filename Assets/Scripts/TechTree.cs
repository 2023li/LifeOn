using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TechNodeData
{
    public string id;                           // 改为 string
    public string name;
    public string description;
    public Sprite icon;
    public int cost;
    public List<string> dependencies = new List<string>();
    public Vector2 position;
}

[CreateAssetMenu(menuName = "LifeOn/TechTree", fileName = "TechTree")]
public class TechTree : ScriptableObject
{
    public List<TechNodeData> techList = new List<TechNodeData>();

    private Dictionary<string, TechNodeData> techDict = new Dictionary<string, TechNodeData>();

    private void OnEnable()
    {
        BuildLookup();
    }

    private void BuildLookup()
    {
        techDict.Clear();
        foreach (var t in techList)
        {
            if (t == null) continue;
            if (string.IsNullOrEmpty(t.id)) continue;
            techDict[t.id] = t;
        }
    }

    public TechNodeData GetTech(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        techDict.TryGetValue(id, out var t);
        return t;
    }

    public bool ContainsTech(string id)
    {
        if (string.IsNullOrEmpty(id)) return false;
        return techDict.ContainsKey(id);
    }

    // 生成不重复的新ID（T001, T002 ...）
    public string GenerateNewId()
    {
        int max = 0;
        foreach (var t in techList)
        {
            if (t == null || string.IsNullOrEmpty(t.id)) continue;
            // 支持 "T###" 格式解析
            if (t.id.StartsWith("T"))
            {
                if (int.TryParse(t.id.Substring(1), out int n))
                    max = Mathf.Max(max, n);
            }
        }
        return $"T{(max + 1).ToString("D3")}";
    }

    public TechNodeData AddTech(string name = "新科技", string description = "", int cost = 0, Sprite icon = null)
    {
        var id = GenerateNewId();
        var node = new TechNodeData
        {
            id = id,
            name = name,
            description = description,
            cost = cost,
            icon = icon,
            dependencies = new List<string>(),
            position = Vector2.zero
        };
        techList.Add(node);
        techDict[id] = node;
        return node;
    }

    public bool RemoveTech(string id)
    {
        var t = GetTech(id);
        if (t == null) return false;
        techList.Remove(t);
        techDict.Remove(id);
        // 清理它在其他节点依赖中的引用
        foreach (var n in techList)
        {
            if (n.dependencies.Contains(id))
                n.dependencies.Remove(id);
        }
        return true;
    }

    public bool AddDependency(string prereqId, string techId)
    {
        var tech = GetTech(techId);
        var pre = GetTech(prereqId);
        if (tech == null || pre == null) return false;
        if (tech.id == prereqId) return false;             // 自己依赖自己
        if (!tech.dependencies.Contains(prereqId))
        {
            tech.dependencies.Add(prereqId);
            return true;
        }
        return false;
    }

    public bool RemoveDependency(string prereqId, string techId)
    {
        var tech = GetTech(techId);
        if (tech == null) return false;
        return tech.dependencies.Remove(prereqId);
    }

    public bool AreDependenciesMet(string techId, HashSet<string> unlocked)
    {
        var t = GetTech(techId);
        if (t == null) return false;
        foreach (var dep in t.dependencies)
            if (!unlocked.Contains(dep)) return false;
        return true;
    }

    public List<TechNodeData> GetAvailableTechs(HashSet<string> unlocked)
    {
        var list = new List<TechNodeData>();
        foreach (var t in techList)
        {
            if (unlocked.Contains(t.id)) continue;
            bool ok = true;
            foreach (var dep in t.dependencies)
            {
                if (!unlocked.Contains(dep)) { ok = false; break; }
            }
            if (ok) list.Add(t);
        }
        return list;
    }
}
