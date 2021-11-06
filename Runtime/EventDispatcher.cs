using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tofunaut.TofuECS
{
    /// <summary>
    /// Allows for concurrent subscribing and unsubscribing to events. Events are queued from the Simulation thread and
    /// Dispatched from the main thread (i.e., the Unity thread). Event data is required to be unmanaged because all state
    /// data in the ECS is unmanaged by design.
    /// </summary>
    internal class EventDispatcher
    {
        private readonly ConcurrentDictionary<Type, List<(object callbackTarget, Action<object> callBack)>> _typeToEvent;
        private readonly ConcurrentQueue<(Type type, object data)> _eventQueue;

        public EventDispatcher()
        {
            _typeToEvent = new ConcurrentDictionary<Type, List<(object, Action<object>)>>();
            _eventQueue = new ConcurrentQueue<(Type type, object data)>();
        }

        public void Subscribe<TEventData>(Action<TEventData> callback) where TEventData : unmanaged
        {
            var callbackList = _typeToEvent.GetOrAdd(typeof(TEventData), new List<(object, Action<object>)>());
            callbackList.Add((callback.Target, data => callback.Invoke((TEventData)data)));
        }

        public void Unsubscribe<TEventData>(Action<TEventData> callback) where TEventData : unmanaged
        {
            if (!_typeToEvent.TryGetValue(typeof(TEventData), out var eventList)) 
                return;

            eventList.RemoveAll(x => x.callbackTarget == callback.Target);
        }

        public void Enqueue<TEventData>(TEventData data) where TEventData : unmanaged =>
            _eventQueue.Enqueue((typeof(TEventData), data));

        public void Dispatch()
        {
            while (_eventQueue.TryDequeue(out var eventData))
            {
                if (_typeToEvent.TryGetValue(eventData.type, out var eventList))
                {
                    foreach (var callbackTuple in eventList)
                        callbackTuple.callBack.Invoke(eventData.data);
                }
            }
        }
    }
}