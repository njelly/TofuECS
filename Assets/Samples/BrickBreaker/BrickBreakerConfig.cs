using System;
using Tofunaut.TofuECS.Math;
using UnityEngine;

namespace Tofunaut.TofuECS.Samples.BrickBreaker
{
    [CreateAssetMenu(fileName = "new BrickBreakerConfig", menuName = "BrickBreaker/Config")]
    public class BrickBreakerConfig : ScriptableObject, ISimulationConfig
    {
        public int MaxRollback => 1;
        public SimulationMode Mode => SimulationMode.Offline;
        public int NumInputs => 1;
        public ulong Seed => _seed;
        public Vector2 BoardExtents => _boardExtents;
        public Fix64 DeltaTime => new Fix64(1) / new Fix64(30);

        [SerializeField] private ulong _seed;
        [SerializeField] private Vector2 _boardExtents;
        
        public TAsset GetAsset<TAsset>(int id)
        {
            return default;
        }
    }
}