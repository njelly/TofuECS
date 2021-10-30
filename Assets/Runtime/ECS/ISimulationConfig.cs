using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tofunaut.TofuECS.Math;

namespace Tofunaut.TofuECS
{
    public interface ISimulationConfig
    {
        /// <summary>
        /// How many frames can the simulation roll back without requiring a snapshot (TODO: snapshot api)
        /// </summary>
        int MaxRollback { get; }

        /// <summary>
        /// Retrieve some data from an asset for use in the simulation with an Id. The Id can be stored on a component, for example.
        /// </summary>
        TData GetECSData<TData>(int id) where TData : unmanaged;

        /// <summary>
        /// The mode of the Simulation.
        /// </summary>
        SimulationMode SimulationMode {get;}
        
        /// <summary>
        /// The type of physics simulation that will run as part of the ECS (if any).
        /// </summary>
        PhysicsMode PhysicsMode { get; }

        /// <summary>
        /// How many separate inputs are there for the simulation? (i.e., players)
        /// </summary>
        int NumInputs { get; }
        
        /// <summary>
        /// The seed for deterministic RNG.
        /// </summary>
        ulong Seed { get; }
        
        /// <summary>
        /// How many ticks per second (i.e., FPS) should the simulation run at?
        /// </summary>
        public int TicksPerSecond { get; }
    }

    public enum SimulationMode
    {
        /// <summary>
        /// The simulation is running on a client, and requires a server running the simulation to verify frames. Rollbacks can occur.
        /// </summary>
        Client,

        /// <summary>
        /// The simulation is running on the server, and can verify frames for clients.
        /// </summary>
        Server,

        /// <summary>
        /// The simulation is not networked, all frames are automatically verified.
        /// </summary>
        Offline,
    }

    public enum PhysicsMode
    {
        /// <summary>
        /// No physics simulation will run as part of the ECS.
        /// </summary>
        None,
        
        /// <summary>
        /// A 2D physics simulation will run. Transform2D and DynamicBody2D components will automatically be registered.
        /// </summary>
        Physics2D,
    }
}
