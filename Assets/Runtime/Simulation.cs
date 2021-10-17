using System;
using System.Collections.Generic;

namespace Tofunaut.TofuECS
{
    public class Simulation
    {
        public ISimulationConfig Config { get; }
        public Frame CurrentFrame { get; private set; }
        public int LastVerifiedFrame { get; private set; }

        private readonly ISystem[] _systems;
        private readonly Frame[] _frames;
        private readonly Dictionary<Type, int> _typeToIndex;
        private readonly InputProvider _inputProvider;
        private readonly Input[] _currentInputs;

        private int _typeIndexCounter;

        public Simulation(ISimulationConfig config, InputProvider inputProvider, ISystem[] systems)
        {
            Config = config;
            _systems = systems;
            _frames = new Frame[config.MaxRollback];

            for (var i = 0; i < _frames.Length; i++)
                _frames[i] = new Frame(this, Config.NumInputs);

            CurrentFrame = _frames[0];

            _typeToIndex = new Dictionary<Type, int>();

            _inputProvider = inputProvider;
            _currentInputs = new Input[Config.NumInputs];
        }

        public void RegisterComponent<TComponent>() where TComponent : unmanaged
        {
            foreach (var f in _frames)
                f.RegisterComponent<TComponent>();

            _typeToIndex.Add(typeof(TComponent), _typeIndexCounter);
            _typeIndexCounter++;
        }

        internal int GetIndexForType(Type type) => _typeToIndex[type];

        public void Tick()
        {
            var prevFrame = CurrentFrame;
            CurrentFrame = _frames[(prevFrame.Number + 1) % _frames.Length];
            CurrentFrame.Recycle(prevFrame);

            for (var i = 0; i < Config.NumInputs; i++)
                _currentInputs[i] = _inputProvider.GetInput(i);

            CurrentFrame.CopyInputs(_currentInputs);

            if (Config.Mode != SimulationMode.Client)
            {
                CurrentFrame.Verify();
                LastVerifiedFrame = CurrentFrame.Number;
            }

            foreach (var system in _systems)
                system.Process(CurrentFrame);
        }

        public void RollbackTo(int frameNumber)
        {
            CurrentFrame = _frames[frameNumber % _frames.Length];
        }
    }
}