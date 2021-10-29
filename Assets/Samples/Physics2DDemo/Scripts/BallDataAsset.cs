using System.Runtime.InteropServices;
using Tofunaut.TofuECS.Math;
using Tofunaut.TofuECS.Samples.Physics2DDemo.ECS;
using Tofunaut.TofuECS.Unity;
using UnityEngine;

namespace Tofunaut.TofuECS.Samples.Physics2DDemo
{
    [CreateAssetMenu(menuName = "Physics2D Demo/Ball Data Asset")]
    public unsafe class BallDataAsset : ECSAsset<BallData>
    {
        [SerializeField] private SerializableFix64 _mass;
        [SerializeField] private SerializableFix64 _radius;
        [SerializeField] private EntityView[] _prefabs;
        
        protected override BallData BuildECSData()
        {
            var prefabIds = (int*)Marshal.AllocHGlobal(Marshal.SizeOf<int>() * _prefabs.Length);
            return new BallData
            {
                Mass = _mass.Value,
                PrefabLength = _prefabs.Length,
                PrefabOptions = prefabIds,
                Radius = _radius.Value
            };
        }
    }
}