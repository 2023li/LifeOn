using System;

[Serializable]
public abstract class StatModifier
{
    public abstract int ModifyMaxPopulation(BuildingInstance self, IGameContext ctx, int current);
}



//[Serializable]
//public class AddMaxPopIfEnvGreater : StatModifier
//{
    
//    public int Threshold = 1;
//    public int AddValue = 3;

//    public override int ModifyMaxPopulation(BuildingInstance self, IGameContext ctx, int current)
//    {
//        int env = ctx.Environment.GetEnvAt(self.GridPos);
//        return env > Threshold ? current + AddValue : current;
//    }
//}
