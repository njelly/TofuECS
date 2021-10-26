using Tofunaut.TofuECS.Math;
using Tofunaut.TofuECS.Samples.Physics2DDemo.ECS;
using Tofunaut.TofuECS.Unity;
using UnityEngine;

namespace Tofunaut.TofuECS.Samples.Physics2DDemo
{
    [CreateAssetMenu(menuName = "Physics2D Demo/Ball Data Asset")]
    public class BallDataAsset : ECSAsset<BallData>
    {
        [SerializeField] private SerializableFix64 _mass;
        [SerializeField] private SerializableFix64 _radius;
        [SerializeField] private EntityView _prefab;
        
        protected override BallData BuildECSData()
        {
            return new BallData
            {
                Mass = _mass.Value,
                PrefabId = _prefab.PrefabId,
                Radius = _radius.Value
            };
        }
    }
}