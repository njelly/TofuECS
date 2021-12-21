namespace Tofunaut.TofuECS
{
    public interface ISimulationConfig
    {
        /// <summary>
        /// How many frames will be stored in memory (the simulation can "rollback" to any frame stored in the rolling buffer).
        /// </summary>
        int FramesInMemory { get; }

        /// <summary>
        /// How many separate inputs are there for the simulation? (i.e., players)
        /// </summary>
        int NumInputs { get; }
        
        /// <summary>
        /// The seed for deterministic RNG.
        /// </summary>
        ulong Seed { get; }
    }
}
