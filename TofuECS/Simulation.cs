using System;
using System.Collections.Generic;
using System.Linq;
using UnsafeCollections.Collections.Unsafe;

namespace Tofunaut.TofuECS
{
    public class Simulation : IDisposable
    {
        public const int InvalidEntityId = 0;
        public int CurrentTick { get; private set; }
        public bool IsInitialized { get; private set; }
        public ILogService Log { get; }
        
        private readonly ISystem[] _systems;
        private readonly Dictionary<Type, IComponentBuffer> _typeToComponentBuffer;
        private readonly Dictionary<Type, ISystem[]> _typeToSystemEventListeners;
        private readonly Dictionary<Type, ComponentQuery> _typeToQueries;
        private int _entityIdCounter;

        public Simulation(ILogService logService, ISystem[] systems)
        {
            Log = logService;
            _systems = systems;
            _typeToComponentBuffer = new Dictionary<Type, IComponentBuffer>();
            _typeToSystemEventListeners = new Dictionary<Type, ISystem[]>();
            _typeToQueries = new Dictionary<Type, ComponentQuery>();
        }

        /// <summary>
        /// Register a component for use in the Simulation.
        /// </summary>
        /// <param name="bufferSize">The size of the buffer, i.e, how many entities can be assigned a component at once.
        /// This cannot be changed after the buffer is created.</param>
        public void RegisterComponent<TComponent>(int bufferSize) where TComponent : unmanaged
        {
            if (IsInitialized)
                throw new SimulationAlreadyInitializedException();

            if (_typeToComponentBuffer.ContainsKey(typeof(TComponent)))
                throw new ComponentAlreadyRegisteredException<TComponent>();

            if (bufferSize < 1)
                throw new InvalidBufferSizeException<TComponent>(bufferSize);
            
            _typeToComponentBuffer.Add(typeof(TComponent), new ComponentBuffer<TComponent>(bufferSize));
        }

        /// <summary>
        /// Register a singleton component for the Simulation (really, just a buffer of size 1). Does not create an entity.
        /// </summary>
        public void RegisterSingletonComponent<TComponent>() where TComponent : unmanaged
        {
            RegisterComponent<TComponent>(1);
            Buffer<TComponent>().SetAt(0, default);
        }

        /// <summary>
        /// Register a singleton component for the Simulation (really, just a buffer of size 1). Allows the value of the
        /// component to be set at the time the buffer is created. Does not create an entity.
        /// </summary>
        public void RegisterSingletonComponent<TComponent>(in TComponent component) where TComponent : unmanaged
        {
            RegisterComponent<TComponent>(1);
            Buffer<TComponent>().SetAt(0, component);
        }

        /// <summary>
        /// Initialize the Simulation. This calls Initialize on all systems sequentially, prevents any more components
        /// from being registered, and allows Tick() to be called.
        /// </summary>
        public void Initialize()
        {
            if (IsInitialized)
                throw new SimulationAlreadyInitializedException();
            
            foreach(var system in _systems)
                system.Initialize(this);

            IsInitialized = true;
        }

        public int CreateEntity() => ++_entityIdCounter;

        /// <summary>
        /// Gets a buffer of the specified type.
        /// </summary>
        public ComponentBuffer<TComponent> Buffer<TComponent>() where TComponent : unmanaged
        {
            ThrowIfBufferDoesntExist<TComponent>(out var buffer);
            return buffer;
        }

        /// <summary>
        /// Gets the value of a singleton component.
        /// </summary>
        public TComponent GetSingletonComponent<TComponent>() where TComponent : unmanaged
        {
            ThrowIfBufferDoesntExist<TComponent>(out var buffer);
            return buffer.GetAt(0);
        }

        /// <summary>
        /// Get a pointer to the value of a singleton component.
        /// </summary>
        public unsafe TComponent* GetSingletonComponentUnsafe<TComponent>() where TComponent : unmanaged
        {
            ThrowIfBufferDoesntExist<TComponent>(out var buffer);
            return buffer.GetAtUnsafe(0);
        }

        /// <summary>
        /// Set the value of a singleton component.
        /// </summary>
        public void SetSingletonComponent<TComponent>(in TComponent component) where TComponent : unmanaged
        {
            ThrowIfBufferDoesntExist<TComponent>(out var buffer);
            buffer.SetAt(0, component);
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
            var buffer = Buffer<TComponent>();
            
            // need to make sure the entityIdCounter is larger than any entity that exists in the current state
            foreach (var entity in entityAssignments)
                _entityIdCounter = Math.Max(entity + 1, _entityIdCounter);

            CurrentTick = currentTick;
            buffer.SetState(components, entityAssignments);
            
            // need to clear the queries, they're no longer valid
            _typeToQueries.Clear();
        }

        // TODO: no op on release builds?
        /// <summary>
        /// Equivalent to Log.Debug(s).
        /// </summary>
        public void Debug(string s) => Log.Debug(s);

        /// <summary>
        /// Raise a system event. All ISystemEventListeners will immediately receive a callback with the event data.
        /// </summary>
        public void SystemEvent<TEvent>(in TEvent eventData) where TEvent : struct
        {
            if (!IsInitialized)
                throw new SimulationNotInitializedException();
            
            if (!_typeToSystemEventListeners.TryGetValue(typeof(TEvent), out var systemEventListeners))
            {
                systemEventListeners = _systems.Where(x => x is ISystemEventListener<TEvent>).ToArray();
                _typeToSystemEventListeners.Add(typeof(TEvent), systemEventListeners);
            }

            // TODO: remove check in release builds?
            if (systemEventListeners.Length == 0)
                throw new NoSystemImplementsSystemEventException<TEvent>();
            
            foreach(var system in systemEventListeners)
                ((ISystemEventListener<TEvent>) system).OnSystemEvent(this, eventData);
        }

        /// <summary>
        /// Get a query for all entities with a component.
        /// </summary>
        public ComponentQuery Query<TComponent>() where TComponent : unmanaged
        {
            if (_typeToQueries.TryGetValue(typeof(TComponent), out var componentQuery)) 
                return componentQuery;
            
            ThrowIfBufferDoesntExist<TComponent>(out var buffer);
            componentQuery = new ComponentQuery(this, buffer);
            _typeToQueries.Add(typeof(TComponent), componentQuery);
            return componentQuery;
        }

        /// <summary>
        /// Increments the value of CurrentTick and calls Process() on all Systems.
        /// </summary>
        public void Tick()
        {
            // TODO: remove check in release builds?
            if (!IsInitialized)
                throw new SimulationNotInitializedException();

            CurrentTick++;

            foreach (var system in _systems)
                system.Process(this);
        }

        private void ThrowIfBufferDoesntExist<TComponent>(out ComponentBuffer<TComponent> buffer)
            where TComponent : unmanaged
        {
            // TODO: remove check in release builds?
            if (!_typeToComponentBuffer.TryGetValue(typeof(TComponent), out var bufferObj) ||
                !(bufferObj is ComponentBuffer<TComponent> componentBuffer))
                throw new ComponentNotRegisteredException<TComponent>();
 
            buffer = componentBuffer;
        }

        public void Dispose()
        {
            foreach(var kvp in _typeToComponentBuffer)
                kvp.Value.Dispose();
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

    public class InvalidBufferSizeException<TComponent> : Exception where TComponent : unmanaged
    {
        public override string Message => $"The size {_size} for buffer of type {typeof(TComponent)} is invalid.";
        private readonly int _size;
        public InvalidBufferSizeException(int size)
        {
            _size = size;
        }
    }
}