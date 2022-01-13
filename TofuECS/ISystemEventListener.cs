namespace Tofunaut.TofuECS
{
    public interface ISystemEventListener<TEvent> where TEvent : struct
    {
        void OnSystemEvent(Simulation s, in TEvent eventData);
    }
}