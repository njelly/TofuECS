using System;
using Tofunaut.TofuECS.Unity;
using UnityEngine;

namespace Tofunaut.TofuECS.Samples.Physics2DDemo
{
    [RequireComponent(typeof(EntityView))]
    public class ShapeView : MonoBehaviour
    {
        private EntityView _entityView;
        private SpriteRenderer _boxView;
        private SpriteRenderer _circleView;
        
        private void Awake()
        {
            _entityView = GetComponent<EntityView>();
        }
    }
}