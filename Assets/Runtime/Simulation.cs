using System;
using System.Collections.Generic;

namespace Tofunaut.TofuECS
{
    public class Simulation
    {
        public ISimulationConfig Config { get; }
        public Frame CurrentFrame { get; private set; }
        public int LastVerifiedFrame { get; private set; }

        private int _entityCounter;
        private ISystem[] _systems;
        private Frame[] _frames;
        private Dictionary<Type, int> _typeToIndex;
        private int _typeIndexCounter;

        public Simulation(ISimulationConfig config, ISystem[] systems)
        {
            Config = config;
            _systems = systems;
            _frames = new Frame[config.MaxRollback];

            for (var i = 0; i < _frames.Length; i++)
                _frames[i] = new Frame(this);

            CurrentFrame = _frames[_frames.Length - 1];

            _typeToIndex = new Dictionary<Type, int>();
        }

        public Entity CreateEntity() => new Entity(_entityCounter++);

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
            CurrentFrame = _frames[frameNumber - 1];
        }
    }
}