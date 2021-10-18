using System;
using System.Collections.Generic;
using Tofunaut.TofuECS.Physics;

namespace Tofunaut.TofuECS
{
    public class Simulation
    {
        public ISimulationConfig Config { get; }
        public Frame CurrentFrame { get; private set; }
        public int LastVerifiedFrame { get; private set; }
        
        public bool IsInitialized { get; private set; }

        private readonly ISystem[] _systems;
        private readonly Frame[] _frames;
        private readonly Dictionary<Type, int> _typeToIndex;
        private readonly InputProvider _inputProvider;
        private readonly Input[] _currentInputs;

        private int _typeIndexCounter;

        public Simulation(ISimulationConfig config, InputProvider inputProvider, ISystem[] systems)
        {
            Config = config;

            var coreSystems = new ISystem[]
            {
                new PhysicsSystem(),
            };
            
            _systems = new ISystem[coreSystems.Length + systems.Length];
            Array.Copy(coreSystems, 0, _systems, 0, coreSystems.Length);
            Array.Copy(systems, 0, _systems, coreSystems.Length, systems.Length);
            
            _frames = new Frame[config.MaxRollback];

            for (var i = 0; i < _frames.Length; i++)
                _frames[i] = new Frame(this, Config.NumInputs);

            CurrentFrame = _frames[0];

            _typeToIndex = new Dictionary<Type, int>();

            _inputProvider = inputProvider;
            _currentInputs = new Input[Config.NumInputs];

            IsInitialized = false;
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

        ~Simulation()
        {
            foreach (var system in _systems)
                system.Dispose(CurrentFrame);
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

        /// <summary>
        /// Process the current frame and go to the next.
        /// </summary>
        public void Tick()
        {
            if (Config.Mode != SimulationMode.Client)
            {
                CurrentFrame.Verify();
                LastVerifiedFrame = CurrentFrame.Number;
            }

            for (var i = 0; i < Config.NumInputs; i++)
                _currentInputs[i] = _inputProvider.GetInput(i);

            CurrentFrame.CopyInputs(_currentInputs);

            foreach (var system in _systems)
                system.Process(CurrentFrame);
            
            // now proceed to the next frame
            var prevFrame = CurrentFrame;
            CurrentFrame = _frames[(prevFrame.Number + 1) % _frames.Length];
            CurrentFrame.Recycle(prevFrame);
        }

        public void RollbackTo(int frameNumber)
        {
            var prevFrameIndex = (frameNumber - 1) % _frames.Length;
            var frameIndex = frameNumber % _frames.Length;
            CurrentFrame = _frames[frameIndex];
            CurrentFrame.Recycle(_frames[prevFrameIndex]);
        }
    }
}