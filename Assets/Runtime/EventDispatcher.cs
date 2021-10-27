using System;
using System.Collections.Generic;

namespace Tofunaut.TofuECS
{
    public class EventDispatcher
    {
        private Dictionary<Type, Dictionary<object, Action<Frame, object>>> _callbacks;

        public EventDispatcher()
        {
            _callbacks = new Dictionary<Type, Dictionary<object, Action<Frame, object>>>();
        }

        public void Subscribe<TEventData>(Action<Frame, TEventData> callback) where TEventData : unmanaged, IDisposable
        {
            if (!_callbacks.TryGetValue(typeof(TEventData), out var callbacks))
            {
                callbacks = new Dictionary<object, Action<Frame, object>>
                {
                    { callback.Target, (f, data) => callback.Invoke(f, (TEventData)data) },
                };
                _callbacks.Add(typeof(TEventData), callbacks);
            }

            callbacks[callback.Target] = (f, data) => callback.Invoke(f, (TEventData)data);
        }

        public void Unsubscribe<TEventData>(Action<Frame, TEventData> callback) where TEventData : unmanaged, IDisposable
        {
            if (!_callbacks.TryGetValue(typeof(TEventData), out var callbacks))
                return;

            callbacks.Remove(callback.Target);
        }

        public void Invoke<TEventData>(Frame f, TEventData data) where TEventData : unmanaged, IDisposable
        {
            if (!_callbacks.TryGetValue(typeof(TEventData), out var callbacks))
                return;

            foreach (var callback in callbacks.Values)
                callback.Invoke(f, data);
            
            data.Dispose();
        }
    }
}