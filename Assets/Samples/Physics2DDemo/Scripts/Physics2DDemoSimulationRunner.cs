using System;
using Tofunaut.TofuECS.Samples.Physics2DDemo.ECS;
using UnityEngine;

namespace Tofunaut.TofuECS.Samples.Physics2DDemo
{
    public class Physics2DDemoSimulationRunner : MonoBehaviour
    {
        [SerializeField] private Physics2DDemoSimulationConfigAsset _config;

        private Simulation _sim;
        private Physics2DDemoInputProvider _demoInputProvider;

        private async void Start()
        {
            await _config.Database.PreloadAll();
            
            _demoInputProvider = new Physics2DDemoInputProvider();
            _sim = new Simulation(_config, _demoInputProvider, new ISystem[]
            {
                new BallSystem(),
                new GravitySystem(),
            });
            
            _sim.Initialize();
        }

        private void Update()
        {
            _sim?.Tick();
        }
    }
}