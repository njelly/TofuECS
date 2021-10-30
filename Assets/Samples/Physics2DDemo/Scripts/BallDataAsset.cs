using System.Runtime.InteropServices;
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
        [SerializeField] private EntityView _entityView;
        
        protected override BallData BuildECSData()
        {
            return new BallData
            {
                Mass = _mass.Value,
                Radius = _radius.Value,
                ViewId = _entityView.PrefabId,
            };
        }
    }
}