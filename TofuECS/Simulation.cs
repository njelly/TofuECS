using System;
using System.Collections.Generic;

namespace Tofunaut.TofuECS
{
    public class Simulation
    {
        public const int InvalidEntityId = 0;
        public int CurrentTick { get; private set; }
        public bool IsInitialized { get; private set; }
        public ECSDatabase DB { get; }
        public ILogService Log { get; }
        public XorShiftRandom RNG { get; }
        
        private readonly ISystem[] _systems;
        private readonly Dictionary<Type, Action<Simulation, object>[]> _typeToSystemEventListenerCallbacks;
        private readonly ExternalEventDispatcher _externalEventDispatcher;
        private readonly Dictionary<Type, object> _typeToComponentBuffer;
        private readonly Dictionary<Type, object> _typeToInputArray;
        private int _entityIdCounter;

        public Simulation(ECSDatabase database, ILogService logService, ulong seed, ISystem[] systems)
        {
            DB = database;
            Log = logService;
            RNG = new XorShiftRandom(seed);
            _systems = systems;
            _typeToComponentBuffer = new Dictionary<Type, object>();
            _typeToInputArray = new Dictionary<Type, object>();
            _typeToSystemEventListenerCallbacks = new Dictionary<Type, Action<Simulation, object>[]>();
            _externalEventDispatcher = new ExternalEventDispatcher();
        }

        public void RegisterComponent<TComponent>(int bufferSize) where TComponent : unmanaged
        {
            if (IsInitialized)
                throw new ECSAlreadyInitializedException();

            if (_typeToComponentBuffer.ContainsKey(typeof(TComponent)))
                throw new ComponentAlreadyRegisteredException<TComponent>();
            
            _typeToComponentBuffer.Add(typeof(TComponent), new ComponentBuffer<TComponent>(bufferSize));
        }

        public void RegisterSingletonComponent<TComponent>(TComponent component = default) where TComponent : unmanaged
        {
            RegisterComponent<TComponent>(1);
            Buffer<TComponent>().Set(CreateEntity(), component);
        }

        public void Initialize()
        {
            if (IsInitialized)
                throw new ECSAlreadyInitializedException();
            
            foreach(var system in _systems)
                system.Initialize(this);

            IsInitialized = true;
        }

        public int CreateEntity() => ++_entityIdCounter;

        public ComponentBuffer<TComponent> Buffer<TComponent>() where TComponent : unmanaged
        {
            if (!_typeToComponentBuffer.TryGetValue(typeof(TComponent), out var bufferObj) ||
                !(bufferObj is ComponentBuffer<TComponent> componentBuffer))
                throw new ComponentNotRegisteredException<TComponent>();

            return componentBuffer;
        }

        public void Subscribe<TEvent>(Action<TEvent> callback) where TEvent : unmanaged =>
            _externalEventDispatcher.Subscribe(callback);

        public void Unsubscribe<TEvent>(Action<TEvent> callback) where TEvent : unmanaged =>
            _externalEventDispatcher.Unsubscribe(callback);

        public void QueueExternalEvent<TEvent>(TEvent eventData) where TEvent : struct
        {
            _externalEventDispatcher.Enqueue(eventData);
        }

        public void RaiseSystemEvent<TEventData>(TEventData eventData) where TEventData : unmanaged
        {
            // cache all callbacks on the first invocation of the system event - is there a better way?
            if (!_typeToSystemEventListenerCallbacks.TryGetValue(typeof(TEventData),
                    out var systemEventListenerCallbacks))
            {
                var callbacks = new List<Action<Simulation, TEventData>>();
                foreach (var system in _systems)
                {
                    if(system is ISystemEventListener<TEventData> systemEventListener)
                        callbacks.Add(systemEventListener.OnSystemEvent);
                }

                systemEventListenerCallbacks = new Action<Simulation, object>[callbacks.Count];
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

        public void SetInput<TInput>(TInput[] inputs) where TInput : unmanaged =>
            _typeToInputArray[typeof(TInput)] = inputs;

        public TInput GetInput<TInput>(int index) where TInput : unmanaged
        {
            if (!_typeToInputArray.TryGetValue(typeof(TInput), out var inputArrayObj) ||
                !(inputArrayObj is TInput[] inputs))
                return default;

            return inputs[index];
        }

        public bool GetSingletonComponent<TComponent>(out TComponent component) where TComponent : unmanaged
        {
            if (!_typeToComponentBuffer.TryGetValue(typeof(TComponent), out var bufferObj) ||
                !(bufferObj is ComponentBuffer<TComponent> componentBuffer))
                throw new ComponentNotRegisteredException<TComponent>();

            return componentBuffer.GetFirst(out component);
        }

        public bool ModifySingletonComponent<TComponent>(ModifyDelegate<TComponent> modifyDelegate)
            where TComponent : unmanaged
        {
            if (!_typeToComponentBuffer.TryGetValue(typeof(TComponent), out var bufferObj) ||
                !(bufferObj is ComponentBuffer<TComponent> componentBuffer))
                throw new ComponentNotRegisteredException<TComponent>();

            return componentBuffer.ModifyFirst(modifyDelegate);
        }

        public void Tick()
        {
            if (!IsInitialized)
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
}