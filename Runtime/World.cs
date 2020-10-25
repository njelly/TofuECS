using System;
using System.Runtime.CompilerServices;
using Tofunaut.TofuECS.Interfaces;

namespace Tofunaut.TofuECS
{
    public class World
    {
        /// <summary>
        /// The most recent frame in the world.
        /// </summary>
        public Frame Predicted { get; private set; }
        
        /// <summary>
        /// The most recent frame to be verified over the network.
        /// </summary>
        public Frame Verified { get; private set; } // TODO: rollback
        
        /// <summary>
        /// Contains initialization data about the ECS World.
        /// </summary>
        public readonly WorldConfig Config;

        /// <summary>
        /// Has the first Update() been called?
        /// </summary>
        public bool HasStarted => Predicted.number > 0;

        private readonly System[] _systems;

        public World(WorldConfig config, System[] systems)
        {
            Config = config;
            Predicted = new Frame(Config.DeltaTime);
            Verified = Predicted;
            _systems = systems;
        }

        public void RegisterComponent<T>() where T : class, IComponent, new()
        {
            if(HasStarted)
                throw new InvalidOperationException("the World has already started, and new component types cannot be registered");
            
            Predicted.RegisterComponent<T>(Config.MaxComponents);
        }

        public void Update()
        {
            Predicted = new Frame(Predicted);
            
            foreach (var system in _systems)
                system.Update(Predicted);
        }
    }
}