using System;

[Serializable]
public abstract class StatModifier
{
    public abstract int Modify(BuildingInstance self, IGameContext ctx, int current);
}



public class Modify_NeedTechnologicalNodes : StatModifier
{
    public string NeedNodesId;

    public int AddValue = 3;

    public override int Modify(BuildingInstance self, IGameContext ctx, int current)
    {
        if (ctx.TechTree.HasNode(NeedNodesId))
        {
           return current + AddValue;
        }
        return current; 

    }
}



//[Serializable]
//public class AddMaxPopIfEnvGreater : StatModifier
//{

//    public int Threshold = 1;
//    public int AddValue = 3;

//    public override int Modify(BuildingInstance self, IGameContext ctx, int current)
//    {
//        int env = ctx.Environment.GetEnvAt(self.GridPos);
//        return env > Threshold ? current + AddValue : current;
//    }
//}
