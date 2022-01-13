using System;
using System.Collections.Generic;
using System.Linq;

namespace Tofunaut.TofuECS
{
    public class Simulation
    {
        public const int InvalidEntityId = 0;
        public int CurrentTick { get; private set; }
        public bool IsInitialized { get; private set; }
        public ILogService Log { get; }
        
        private readonly ISystem[] _systems;
        private readonly Dictionary<Type, object> _typeToComponentBuffer;
        private readonly Dictionary<Type, ISystem[]> _typeToSystemEventListeners;
        private int _entityIdCounter;

        public Simulation(ILogService logService, ISystem[] systems)
        {
            Log = logService;
            _systems = systems;
            _typeToComponentBuffer = new Dictionary<Type, object>();
            _typeToSystemEventListeners = new Dictionary<Type, ISystem[]>();
        }

        public void RegisterComponent<TComponent>(int bufferSize) where TComponent : unmanaged
        {
            ThrowIfInvalidComponentRegistration<TComponent>();
            _typeToComponentBuffer.Add(typeof(TComponent), new ComponentBuffer<TComponent>(bufferSize));
        }

        public void RegisterSingletonComponent<TComponent>() where TComponent : unmanaged
        {
            ThrowIfInvalidComponentRegistration<TComponent>();
            RegisterComponent<TComponent>(1);
            Buffer<TComponent>().Set(CreateEntity());
        }

        public void RegisterSingletonComponent<TComponent>(in TComponent component) where TComponent : unmanaged
        {
            ThrowIfInvalidComponentRegistration<TComponent>();
            RegisterComponent<TComponent>(1);
            Buffer<TComponent>().Set(CreateEntity(), component);
        }

        private void ThrowIfInvalidComponentRegistration<TComponent>() where TComponent : unmanaged
        {
            if (IsInitialized)
                throw new SimulationAlreadyInitializedException();

            if (_typeToComponentBuffer.ContainsKey(typeof(TComponent)))
                throw new ComponentAlreadyRegisteredException<TComponent>();
        }

        public void Initialize()
        {
            if (IsInitialized)
                throw new SimulationAlreadyInitializedException();
            
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

        public bool ModifySingletonComponentUnsafe<TComponent>(ModifyDelegateUnsafe<TComponent> modifyDelegateUnsafe)
            where TComponent : unmanaged
        {
            if (!_typeToComponentBuffer.TryGetValue(typeof(TComponent), out var bufferObj) ||
                !(bufferObj is ComponentBuffer<TComponent> componentBuffer))
                throw new ComponentNotRegisteredException<TComponent>();

            return componentBuffer.ModifyFirstUnsafe(modifyDelegateUnsafe);
        }
        
        /// <summary>
        /// Gets the entire state of the buffer.
        /// </summary>
        /// <param name="components">An array of <typeparam name="TComponent"></typeparam>> representing the current state of the buffer.</param>
        /// <param name="entityAssignments">An array of integers representing entity assignments at each buffer index.</param>
        /// <exception cref="ComponentNotRegisteredException{TComponent}">Will be thrown if the component has not been registered.</exception>
        public void GetState<TComponent>(out TComponent[] components, out int[] entityAssignments) 
            where TComponent : unmanaged
        {
            if (!_typeToComponentBuffer.TryGetValue(typeof(TComponent), out var bufferObj) ||
                !(bufferObj is ComponentBuffer<TComponent> componentBuffer))
                throw new ComponentNotRegisteredException<TComponent>();

            componentBuffer.GetState(out components, out entityAssignments);
        }

        /// <summary>
        /// Overwrites the entire state of the buffer.
        /// </summary>
        /// <param name="components">An array of <typeparam name="TComponent"></typeparam>> that will become the new state of the buffer.</param>
        /// <param name="entityAssignments">An array of integers representing entity assignments at each buffer index.</param>
        /// <param name="currentTick">Sets the current tick value.</param>
        /// <exception cref="ComponentNotRegisteredException{TComponent}">Will be thrown if the component has not been registered.</exception>
        public void SetState<TComponent>(TComponent[] components, int[] entityAssignments, int currentTick) where TComponent : unmanaged
        {
            if (!_typeToComponentBuffer.TryGetValue(typeof(TComponent), out var bufferObj) ||
                !(bufferObj is ComponentBuffer<TComponent> componentBuffer))
                throw new ComponentNotRegisteredException<TComponent>();
            
            // need to make sure the entityIdCounter is larger than any entity that exists in the current state
            foreach (var entity in entityAssignments)
                _entityIdCounter = Math.Max(entity + 1, _entityIdCounter);

            CurrentTick = currentTick;
            componentBuffer.SetState(components, entityAssignments);
        }

        // TODO: no op on release builds?
        public void Debug(string s) => Log.Debug(s);

        public void SystemEvent<TEvent>(in TEvent eventData) where TEvent : struct
        {
            if (!_typeToSystemEventListeners.TryGetValue(typeof(TEvent), out var systemEventListeners))
            {
                systemEventListeners = _systems.Where(x => x is ISystemEventListener<TEvent>).ToArray();
                _typeToSystemEventListeners.Add(typeof(TEvent), systemEventListeners);
            }

            if (systemEventListeners.Length == 0)
                throw new NoSystemImplementsSystemEventException<TEvent>();
            
            foreach(var system in systemEventListeners)
                ((ISystemEventListener<TEvent>) system).OnSystemEvent(this, eventData);
        }

        public void Tick()
        {
            if (!IsInitialized)
                throw new SimulationNotInitializedException();

            CurrentTick++;

            foreach (var system in _systems)
                system.Process(this);
        }
    }

    public class NoSystemImplementsSystemEventException<TEvent> : Exception where TEvent : struct
    {
        public override string Message => $"No system listens to system event of type {typeof(TEvent)}";
    }

    public class SimulationNotInitializedException : Exception
    {
        public override string Message => "The simulation must be initialized.";
    }

    public class SimulationAlreadyInitializedException : Exception
    {
        public override string Message => "The simulation is already initialized.";
    }

    public class ComponentNotRegisteredException<TComponent> : Exception where TComponent : unmanaged
    {
        public override string Message => $"The component of type {typeof(TComponent)} is not registered.";
    }

    public class ComponentAlreadyRegisteredException<TComponent> : Exception where TComponent : unmanaged
    {
        public override string Message => $"The component of type {typeof(TComponent)} is already registered.";
    }
}