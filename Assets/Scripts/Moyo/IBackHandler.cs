using Moyo.Unity;
using UnityEngine.Rendering;

namespace Moyo.Unity
{
    public interface IBackHandler
    {
        int Priority { get; }

        bool TryHandleBack();
    }
}





public class IBackRegister
{

    public const int UIPriority = 100;
    public const int BuildingBack = 50;
    public const int GameBack = 20;

    public class UIBackHandler : IBackHandler
    {
        public int Priority => UIPriority;

        public bool TryHandleBack()
        {
            return UIManager.Instance.BackTopPanel();
        }
    }

    public class BuidingBackHandle : IBackHandler
    {
        public int Priority => BuildingBack;

        public bool TryHandleBack()
        {
            return BuildingBuilder.Instance.TryHandleBack();
        }
    }

    public class GameBackHandle : IBackHandler
    {
        public int Priority => GameBack;

        public bool TryHandleBack()
        {
            TheGame.Instance.Pause();
            return true;
        }
    }

}
