using Tofunaut.TofuECS.Math;
using Tofunaut.TofuECS.Samples.Physics2DDemo.ECS;
using Tofunaut.TofuECS.Unity;
using UnityEngine;

namespace Tofunaut.TofuECS.Samples.Physics2DDemo
{
    [CreateAssetMenu(menuName = "Physics2D Demo/Simulation Config")]
    public class Physics2DDemoSimulationConfigAsset : ScriptableObject, IPhysics2DSimulationConfig
    {
        public int MaxRollback => 60;
        public SimulationMode SimulationMode => SimulationMode.Offline;
        public PhysicsMode PhysicsMode => PhysicsMode.Physics2D;
        public int NumInputs => 1;
        public int TicksPerSecond => 60;
        public ulong Seed => _seed;

        public BallData BallData => GetECSData<BallData>(_ballDataAsset.AssetId);
        public FixVector2 Gravity => _gravityForce.Value;

        public ECSDatabase Database => _database;

        [SerializeField] private ulong _seed;
        [SerializeField] private ECSDatabase _database;
        [SerializeField] private BallDataAsset _ballDataAsset;
        [SerializeField] private SerializableFixVector2 _gravityForce;
        [SerializeField] private SerializableFix64 _deltaTime;
        
        public TData GetECSData<TData>(int id) where TData : unmanaged
        {
            return _database.GetECSData<TData>(id);
        }
    }
}