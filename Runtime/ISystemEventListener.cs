using System;

namespace Tofunaut.TofuECS
{
    public interface ISystemEventListener<in TEventData> where TEventData : unmanaged
    {
        void OnSystemEvent(Frame f, TEventData data);
    }
}