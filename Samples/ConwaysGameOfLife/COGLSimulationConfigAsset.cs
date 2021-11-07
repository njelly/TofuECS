using Tofunaut.TofuECS.Unity;
using UnityEngine;

namespace Tofunaut.TofuECS.Samples.ConwaysGameOfLife.ECS
{
    [CreateAssetMenu(menuName = "ConwaysGameOfLife/COGL SimulationConfigAsset")]
    public class COGLSimulationConfigAsset : ScriptableObject,  ICOGLSimulationConfig
    {
        public int FramesInMemory => 2;
        public int NumInputs => 1;
        public ulong Seed => _seed;
        public int BoardConfigId => _boardConfigAsset.AssetId;

        [SerializeField] private ulong _seed;
        [SerializeField] private BoardConfigAsset _boardConfigAsset;
        [SerializeField] private ECSDatabase _ecsDatabase;

        public TData GetECSData<TData>(int id) where TData : unmanaged => _ecsDatabase.GetECSData<TData>(id);
    }
}