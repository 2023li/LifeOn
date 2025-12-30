namespace Moyo.Unity
{
    public interface IBackHandler
    {
        int Priority { get; }

        bool TryHandleBack();
    }
}
