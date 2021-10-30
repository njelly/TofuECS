using Tofunaut.TofuECS.Math;
using UnityEngine;

namespace Tofunaut.TofuECS.Samples.Breakout
{
    [CreateAssetMenu(menuName = "Breakout/Simulation Config")]
    public class BreakoutSimulationConfigAsset : ScriptableObject, ISimulationConfig
    {
        public int MaxRollback => 1;
        public SimulationMode SimulationMode => SimulationMode.Offline;
        public PhysicsMode PhysicsMode => PhysicsMode.None;
        public int NumInputs => 1;
        public ulong Seed => _seed;
        public int TicksPerSecond => 60;

        [SerializeField] private ulong _seed;
        
        public TData GetECSData<TData>(int id) where TData : unmanaged
        {
            throw new System.NotImplementedException();
        }
    }
}