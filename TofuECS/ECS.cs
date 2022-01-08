using System;
using System.Collections.Generic;

namespace Tofunaut.TofuECS
{
    public class ECS
    {
        public const int InvalidEntityId = 0;
        public int CurrentTick { get; private set; }
        public bool Initialized { get; private set; }
        public IECSDatabase Database { get; }
        public ILogService Log { get; }
        public XorShiftRandom RNG { get; }
        
        private readonly ISystem[] _systems;
        private readonly Dictionary<Type, object> _typeToComponentBuffer;
        private readonly Dictionary<Type, Action<ECS, object>[]> _typeToSystemEventListenerCallbacks;
        private readonly ExternalEventDispatcher _externalEventDispatcher;
        private int _entityIdCounter;

        public ECS(IECSDatabase database, ILogService logService, ulong seed, ISystem[] systems)
        {
            Database = database;
            Log = logService;
            RNG = new XorShiftRandom(seed);
            _systems = systems;
            _typeToComponentBuffer = new Dictionary<Type, object>();
            _entityIdCounter = InvalidEntityId;
            _typeToSystemEventListenerCallbacks = new Dictionary<Type, Action<ECS, object>[]>();
            _externalEventDispatcher = new ExternalEventDispatcher();
        }

        public void RegisterComponent<TComponent>(int bufferLength, bool canExpand = true) where TComponent : unmanaged
        {
            if (Initialized)
                throw new ECSAlreadyInitializedException();

            if (_typeToComponentBuffer.ContainsKey(typeof(TComponent)))
                throw new ComponentAlreadyRegisteredException<TComponent>();
            
            _typeToComponentBuffer.Add(typeof(TComponent), new ComponentBuffer<TComponent>(bufferLength, canExpand));
        }

        public void Initialize()
        {
            if (Initialized)
                throw new ECSAlreadyInitializedException();
            
            foreach(var system in _systems)
                system.Initialize(this);

            Initialized = true;
        }

        public void AssignComponent<TComponent>(int entityId, TComponent component = default) where TComponent : unmanaged
        {
            if (!_typeToComponentBuffer.TryGetValue(typeof(TComponent), out var componentBufferObj))
                throw new ComponentNotRegisteredException<TComponent>();
            
            ((ComponentBuffer<TComponent>) componentBufferObj).Request(entityId, component);
        }

        public void RemoveComponent<TComponent>(int entityId) where TComponent : unmanaged
        {
            if (!_typeToComponentBuffer.TryGetValue(typeof(TComponent), out var componentBufferObj))
                throw new ComponentNotRegisteredException<TComponent>();
            
            ((ComponentBuffer<TComponent>) componentBufferObj).Release(entityId);
        }

        public TComponent Get<TComponent>(int entityId) where TComponent : unmanaged
        {
            if (!_typeToComponentBuffer.TryGetValue(typeof(TComponent), out var componentBufferObj))
                throw new ComponentNotRegisteredException<TComponent>();
            
            return ((ComponentBuffer<TComponent>) componentBufferObj).Get(entityId);
        }

        public unsafe TComponent* GetUnsafe<TComponent>(int entityId) where TComponent : unmanaged
        {
            if (!_typeToComponentBuffer.TryGetValue(typeof(TComponent), out var componentBufferObj))
                throw new ComponentNotRegisteredException<TComponent>();
            
            return ((ComponentBuffer<TComponent>) componentBufferObj).GetUnsafe(entityId);
        }

        public EntityComponentIterator<TComponent> GetIterator<TComponent>() where TComponent : unmanaged
        {
            if (!_typeToComponentBuffer.TryGetValue(typeof(TComponent), out var componentBufferObj))
                throw new ComponentNotRegisteredException<TComponent>();

            var componentBuffer = componentBufferObj as ComponentBuffer<TComponent>;
            componentBuffer.ResetIterator();
            return new EntityComponentIterator<TComponent>(componentBuffer);
        }

        public void Subscribe<TEvent>(Action<TEvent> callback) where TEvent : unmanaged =>
            _externalEventDispatcher.Subscribe(callback);

        public void Unsubscribe<TEvent>(Action<TEvent> callback) where TEvent : unmanaged =>
            _externalEventDispatcher.Unsubscribe(callback);

        public void QueueExternalEvent<TEvent>(TEvent eventData) where TEvent : unmanaged
        {
            _externalEventDispatcher.Enqueue(eventData);
        }

        public void RaiseSystemEvent<TEventData>(TEventData eventData) where TEventData : unmanaged
        {
            // cache all callbacks on the first invocation of the system event - is there a better way?
            if (!_typeToSystemEventListenerCallbacks.TryGetValue(typeof(TEventData),
                    out var systemEventListenerCallbacks))
            {
                var callbacks = new List<Action<ECS, TEventData>>();
                foreach (var system in _systems)
                {
                    if(system is ISystemEventListener<TEventData> systemEventListener)
                        callbacks.Add(systemEventListener.OnSystemEvent);
                }

                systemEventListenerCallbacks = new Action<ECS, object>[callbacks.Count];
                for (var i = 0; i < callbacks.Count; i++)
                {
                    var index = i;
                    systemEventListenerCallbacks[i] = (frame, data) =>
                    {
                        callbacks[index].Invoke(frame, (TEventData)data);
                    };
                }
                
                _typeToSystemEventListenerCallbacks.Add(typeof(TEventData), systemEventListenerCallbacks);
            }
            
            foreach(var systemEventListenerCallback in systemEventListenerCallbacks)
                systemEventListenerCallback.Invoke(this, eventData);
        }

        public void Tick()
        {
            if (!Initialized)
                throw new ECSNotInitializedException();

            CurrentTick++;

            foreach (var system in _systems)
                system.Process(this);
            
            _externalEventDispatcher.Dispatch();
        }

        public int CreateEntity() => ++_entityIdCounter;
    }

    public class ECSNotInitializedException : Exception
    {
        public override string Message => "Unable to perform the operation, the ECS must be initialized.";
    }

    public class ECSAlreadyInitializedException : Exception
    {
        public override string Message => "Unable to perform the operation, the ECS is already initialized.";
    }

    public class ComponentNotRegisteredException<TComponent> : Exception
    {
        public override string Message => $"The component of type {typeof(TComponent)} has not been registered.";
    }

    public class ComponentAlreadyRegisteredException<TComponent> : Exception
    {
        public override string Message => $"The component of type {typeof(TComponent)} is already registered.";
    }
}