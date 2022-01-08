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
        private readonly Dictionary<int, Dictionary<Type, int>> _entityToTypeToComponentIndex;
        private readonly ExternalEventDispatcher _externalEventDispatcher;
        private int _entityIdCounter;

        public ECS(IECSDatabase database, ILogService logService, ulong seed, ISystem[] systems)
        {
            Database = database;
            Log = logService;
            RNG = new XorShiftRandom(seed);
            _systems = systems;
            _typeToComponentBuffer = new Dictionary<Type, object>();
            _typeToSystemEventListenerCallbacks = new Dictionary<Type, Action<ECS, object>[]>();
            _entityToTypeToComponentIndex = new Dictionary<int, Dictionary<Type, int>>();
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

        public int CreateEntity() => ++_entityIdCounter;

        public void AssignComponent<TComponent>(int entityId, TComponent component = default) where TComponent : unmanaged
        {
            if (!_typeToComponentBuffer.TryGetValue(typeof(TComponent), out var componentBufferObj))
                throw new ComponentNotRegisteredException<TComponent>();
            
            var componentIndex = ((ComponentBuffer<TComponent>) componentBufferObj).Request(entityId, component);

            if (!_entityToTypeToComponentIndex.TryGetValue(entityId, out var typeToComponentIndex))
            {
                typeToComponentIndex = new Dictionary<Type, int>();
                _entityToTypeToComponentIndex.Add(entityId, typeToComponentIndex);
            }
            
            typeToComponentIndex.Add(typeof(TComponent), componentIndex);
        }

        public void RemoveComponent<TComponent>(int entityId) where TComponent : unmanaged
        {
            if (!_typeToComponentBuffer.TryGetValue(typeof(TComponent), out var componentBufferObj))
                throw new ComponentNotRegisteredException<TComponent>();

            if (!_entityToTypeToComponentIndex.TryGetValue(entityId, out var typeToComponentIndex))
                return;

            if (!(componentBufferObj is ComponentBuffer<TComponent> componentBuffer))
                return;
            
            componentBuffer.Release(typeToComponentIndex[typeof(TComponent)]);
            typeToComponentIndex.Remove(typeof(TComponent));
        }

        public TComponent Get<TComponent>(int entityId) where TComponent : unmanaged
        {
            if (!_entityToTypeToComponentIndex.TryGetValue(entityId, out var typeToComponentIndex) ||
                !typeToComponentIndex.TryGetValue(typeof(TComponent), out var componentIndex))
                throw new ComponentNotAssignedException<TComponent>(entityId);

            if (!(_typeToComponentBuffer[typeof(TComponent)] is ComponentBuffer<TComponent> componentBuffer))
                throw new ComponentNotRegisteredException<TComponent>();

            return componentBuffer.Get(componentIndex);
        }

        public unsafe TComponent* GetUnsafe<TComponent>(int entityId) where TComponent : unmanaged
        {
            if (!_entityToTypeToComponentIndex.TryGetValue(entityId, out var typeToComponentIndex) ||
                !typeToComponentIndex.TryGetValue(typeof(TComponent), out var componentIndex))
                throw new ComponentNotAssignedException<TComponent>(entityId);

            if (!(_typeToComponentBuffer[typeof(TComponent)] is ComponentBuffer<TComponent> componentBuffer))
                throw new ComponentNotRegisteredException<TComponent>();

            return componentBuffer.GetUnsafe(componentIndex);
        }

        public EntityComponentIterator<TComponent> GetIterator<TComponent>() where TComponent : unmanaged
        {
            if (!_typeToComponentBuffer.TryGetValue(typeof(TComponent), out var componentBufferObj))
                throw new ComponentNotRegisteredException<TComponent>();

            if (!(componentBufferObj is ComponentBuffer<TComponent> componentBuffer))
                return null;
            
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
    }

    public class ECSNotInitializedException : Exception
    {
        public override string Message => "Unable to perform the operation, the ECS must be initialized.";
    }

    public class ECSAlreadyInitializedException : Exception
    {
        public override string Message => "Unable to perform the operation, the ECS is already initialized.";
    }

    public class ComponentNotRegisteredException<TComponent> : Exception where TComponent : unmanaged
    {
        public override string Message => $"The component of type {typeof(TComponent)} has not been registered.";
    }

    public class ComponentAlreadyRegisteredException<TComponent> : Exception where TComponent : unmanaged
    {
        public override string Message => $"The component of type {typeof(TComponent)} is already registered.";
    }

    public class ComponentNotAssignedException<TComponent> : Exception where TComponent : unmanaged
    {
        public override string Message =>
            $"The component of type {typeof(TComponent)} is not assigned to entity {_entityId}.";
        private readonly int _entityId;

        public ComponentNotAssignedException(int entityId)
        {
            _entityId = entityId;
        }
    }
}