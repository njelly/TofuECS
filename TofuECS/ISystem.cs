using System;
using System.Collections.Generic;

namespace Tofunaut.TofuECS
{
    public interface ISystem
    {
        /// <summary>
        /// Called in the constructor of the simulation
        /// </summary>
        void Initialize(Frame f);
        
        /// <summary>
        /// Called once every Tick()
        /// </summary>
        void Process(Frame f);

        /// <summary>
        /// Called when the simulation is disposed
        /// </summary>
        void Dispose(Frame f);
    }
}