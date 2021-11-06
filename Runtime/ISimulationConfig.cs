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

        /// <summary>
        /// Retrieve some data from an asset for use in the simulation with an Id. The Id can be stored on a component, for example.
        /// </summary>
        TData GetECSData<TData>(int id) where TData : unmanaged;
    }
}
