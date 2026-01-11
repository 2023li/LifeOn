using Moyo.Unity;
using UnityEngine.Rendering;

namespace Moyo.Unity
{
    public interface IBackHandler
    {
        int BackPriority { get; }

        bool TryHandleBack();
    }
}
public static class BackPrioritySort
{
    public const int UIPriority = 100;
    public const int BuildingBack = 50;
    public const int GameBack = 20;
}




public class IBackRegister
{

    public const int UIPriority = 100;
    public const int BuildingBack = 50;
    public const int GameBack = 20;

  

    public class BuidingBackHandle : IBackHandler
    {
        public int BackPriority => BuildingBack;

        public bool TryHandleBack()
        {
            return BuildingBuilder.Instance.TryHandleBack();
        }
    }

    

}
