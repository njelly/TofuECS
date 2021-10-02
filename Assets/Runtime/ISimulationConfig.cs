using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tofunaut.TofuECS
{
    public interface ISimulationConfig
    {
        /// <summary>
        /// How many frames can the simulation roll back without requiring a snapshot (TODO: snapshot api)
        /// </summary>
        int MaxRollback { get; }

        /// <summary>
        /// Retrieve some asset for use in the simulation with an Id. The Id can be stored on a component, for example.
        /// </summary>
        TAsset GetAsset<TAsset>(int id);

        /// <summary>
        /// The mode of the Simulation.
        /// </summary>
        SimulationMode Mode {get;}
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
}
