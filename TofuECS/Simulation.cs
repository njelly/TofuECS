using System;
using System.Collections.Generic;

namespace Tofunaut.TofuECS
{
    public class Simulation
    {
        public ISimulationConfig Config { get; }
        public IECSDatabase Database { get; }
        public Frame CurrentFrame { get; private set; }
        public bool IsInitialized { get; private set; }
        public int HighestProcessedFrameNumber { get; private set; }
        internal ILogService Log { get; }
        internal EventDispatcher EventDispatcher { get; }

        private readonly ISystem[] _systems;
        private readonly Frame[] _frames;
        private readonly Dictionary<Type, int> _typeToIndex;
        private readonly Dictionary<Type, object[]> _typeToInput;
        private readonly Dictionary<Type, Action<Frame, object>[]> _typeToSystemEventListenerCallbacks;
        private int _typeIndexCounter;

        public Simulation(ISimulationConfig config, IECSDatabase database, ILogService log, ISystem[] systems)
        {
            Config = config;
            Database = database;
            Log = log;
            EventDispatcher = new EventDispatcher();
            IsInitialized = false;

            _typeToIndex = new Dictionary<Type, int>();
            _systems = systems;
            _frames = new Frame[config.FramesInMemory];
            _typeToInput = new Dictionary<Type, object[]>();
            _typeToSystemEventListenerCallbacks = new Dictionary<Type, Action<Frame, object>[]>();
            if(_frames.Length < 2)
                log.Error("The simulation must have at least 2 frames to work properly.");

            for (var i = 0; i < _frames.Length; i++)
                _frames[i] = new Frame(this, Config.NumInputs);
            
            CurrentFrame = _frames[0];
            HighestProcessedFrameNumber = -1;
        }

        /// <summary>
        /// Call Initialize() on every system in the Simulation. Allows RegisterComponent() to be called without exception.
        /// </summary>
        public void Initialize()
        {
            foreach (var system in _systems)
                system.Initialize(CurrentFrame);
            
            IsInitialized = true;
        }

        public void Shutdown()
        {
            IsInitialized = false;
            
            foreach (var system in _systems)
                system.Dispose(CurrentFrame);
            
            foreach (var frame in _frames)
                frame.Dispose();
        }

        ~Simulation()
        {
            if(IsInitialized)
                Shutdown();
        }

        public void Subscribe<TEventData>(Action<TEventData> callback) where TEventData : unmanaged =>
            EventDispatcher.Subscribe(callback);

        public void Unsubscribe<TEventData>(Action<TEventData> callback) where TEventData : unmanaged =>
            EventDispatcher.Unsubscribe(callback);

        public void PollEvents() => EventDispatcher.Dispatch();

        public void InjectNewInput<TInput>(TInput[] input) where TInput : unmanaged
        {
            if (!_typeToInput.TryGetValue(typeof(TInput), out var inputArray))
            {
                inputArray = new object[input.Length];
                _typeToInput.Add(typeof(TInput), inputArray);
            }

            for (var i = 0; i < input.Length; i++)
                inputArray[i] = input[i];
        }

        public void InjectInputForFrame<TInput>(TInput[] input, int frameNumber) where TInput : unmanaged
        {
            var frame = _frames[frameNumber % _frames.Length];
            if (frame.Number != frameNumber)
            {
                // TODO: implement snapshots
                Log.Error($"the frame {frameNumber} no longer exists");
                return;
            }

            var inputObjArray = new object[input.Length];
            for (var i = 0; i < input.Length; i++)
                inputObjArray[i] = input[i];
            
            frame.SetInput(typeof(TInput), inputObjArray);
        }

        /// <summary>
        /// Register a component and allow it to be added to an Entity. Will throw SimulationIsNotInitializedException if Initialize() has not been called.
        /// </summary>
        public void RegisterComponent<TComponent>() where TComponent : unmanaged
        {
            foreach (var f in _frames)
                f.RegisterComponent<TComponent>();

            _typeToIndex.Add(typeof(TComponent), _typeIndexCounter);
            _typeIndexCounter++;
        }

        internal int GetIndexForType(Type type) => _typeToIndex[type];

        internal void RaiseSystemEvent<TEventData>(Frame f, TEventData eventData) where TEventData : unmanaged
        {
            // cache all callbacks on the first invocation of the system event - is there a better way?
            if (!_typeToSystemEventListenerCallbacks.TryGetValue(typeof(TEventData),
                out var systemEventListenerCallbacks))
            {
                var callbacks = new List<Action<Frame, TEventData>>();
                foreach (var system in _systems)
                {
                    if(system is ISystemEventListener<TEventData> systemEventListener)
                        callbacks.Add(systemEventListener.OnSystemEvent);
                }

                systemEventListenerCallbacks = new Action<Frame, object>[callbacks.Count];
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
                systemEventListenerCallback.Invoke(f, eventData);
        }

        /// <summary>
        /// Process the current frame and go to the next.
        /// </summary>
        public void Tick()
        {
            foreach (var kvp in _typeToInput)
                CurrentFrame.SetInput(kvp.Key, kvp.Value);
                
            _typeToInput.Clear();
            
            foreach (var system in _systems)
                system.Process(CurrentFrame);
            
            HighestProcessedFrameNumber = Math.Max(HighestProcessedFrameNumber, CurrentFrame.Number);
            
            EventDispatcher.Dispatch();
            
            // now proceed to the next frame
            var prevFrame = CurrentFrame;
            CurrentFrame = _frames[(prevFrame.Number + 1) % _frames.Length];
            CurrentFrame.Recycle(prevFrame);
        }  

        public void RollbackTo(int frameNumber)
        {
            if (frameNumber < 0 || frameNumber > HighestProcessedFrameNumber || HighestProcessedFrameNumber - frameNumber >= _frames.Length - 2)
                throw new InvalidRollbackException(CurrentFrame.Number, frameNumber);
            
            var prevFrameIndex = (frameNumber - 1) % _frames.Length;
            var frameIndex = frameNumber % _frames.Length;
            CurrentFrame = _frames[frameIndex];
            CurrentFrame.Recycle(_frames[prevFrameIndex]);
        }
    }

    public class InvalidRollbackException : Exception
    {
        public override string Message =>
            $"The current frame is {_currentFrame}, the simulation cannot rollback to frame {_rollbackFrame}.";
        private readonly int _currentFrame, _rollbackFrame;
        public InvalidRollbackException(int currentFrame, int rollbackFrame)
        {
            _currentFrame = currentFrame;
            _rollbackFrame = rollbackFrame;
        }
    }
}