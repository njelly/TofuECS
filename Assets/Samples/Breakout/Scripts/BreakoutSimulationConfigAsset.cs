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
        public Fix64 DeltaTime => Fix64.FROM_FLOAT_UNSAFE(Time.deltaTime);

        [SerializeField] private ulong _seed;
        
        public TData GetECSData<TData>(int id) where TData : unmanaged
        {
            throw new System.NotImplementedException();
        }
    }
}