namespace Tofunaut.TofuECS
{
    public interface ISystemEventListener<in TEvent> where TEvent : unmanaged
    {
        void OnSystemEvent(Simulation simulation, TEvent data);
    }
}