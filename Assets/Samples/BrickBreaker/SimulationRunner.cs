using System;
using UnityEngine;

namespace Tofunaut.TofuECS.Samples.BrickBreaker
{
    public class SimulationRunner : MonoBehaviour
    {
        [SerializeField] private BrickBreakerConfig _config;
        
        private Simulation _sim;
        private BrickBreakerInputProvider _inputProvider;

        private void Start()
        {
            _inputProvider = new BrickBreakerInputProvider();
            _sim = new Simulation(_config, _inputProvider, new ISystem[]
            {
                new PaddleSystem(),
            });
            
            _sim.RegisterComponent<Paddle>();
            _sim.RegisterComponent<Brick>();
            _sim.RegisterComponent<Ball>();
        }

        private void Update()
        {
            _sim.Tick();
        }
    }
}