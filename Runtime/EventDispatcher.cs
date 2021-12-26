using System;
using System.Collections.Generic;

namespace Tofunaut.TofuECS
{
    internal class EventDispatcher
    {
        private readonly Dictionary<Type, List<(object callbackTarget, Action<object> callBack)>> _typeToEvent;
        private readonly Queue<(Type type, object data)> _eventQueue;

        public EventDispatcher()
        {
            _typeToEvent = new Dictionary<Type, List<(object, Action<object>)>>();
            _eventQueue = new Queue<(Type type, object data)>();
        }

        public void Subscribe<TEventData>(Action<TEventData> callback) where TEventData : unmanaged
        {
            if (!_typeToEvent.TryGetValue(typeof(TEventData), out var callbackList))
            {
                callbackList = new List<(object, Action<object>)>();
                _typeToEvent.Add(typeof(TEventData), callbackList);
            }
            
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
            while (_eventQueue.Count > 0)
            {
                var (type, data) = _eventQueue.Dequeue();
                if (!_typeToEvent.TryGetValue(type, out var eventList)) 
                    continue;
                
                foreach (var callbackTuple in eventList)
                    callbackTuple.callBack.Invoke(data);
            }
        }
    }
}