using Tofunaut.TofuECS.Samples.Physics2DDemo.ECS;
using Tofunaut.TofuECS.Unity;
using Tofunaut.TofuECS.Utilities;
using UnityEngine;

namespace Tofunaut.TofuECS.Samples.Physics2DDemo
{
    public class Physics2DDemoSimulationRunner : MonoBehaviour
    {
        [SerializeField] private Physics2DDemoSimulationConfigAsset _config;

        private Simulation _sim;
        private Physics2DDemoInputProvider _demoInputProvider;
        private EntityViewManager _entityViewManager;

        private async void Start()
        {
            await _config.Database.PreloadAll();
            
            _demoInputProvider = new Physics2DDemoInputProvider();
            _entityViewManager = new EntityViewManager(_config.Database);
            
            _sim = new Simulation(_config, new UnityLogService(), _demoInputProvider, new ISystem[]
            {
                new ViewIdSystem(),
                new BallSystem(),
                new GravitySystem(),
            });
            
            _sim.RegisterComponent<ViewId>();
            
            _sim.Subscribe<OnViewIdChangedEvent>(OnViewIdChanged);
            _sim.Subscribe<OnEntityDestroyedEvent>(OnEntityDestroyed);
            
            _sim.Initialize();
        }

        private void Update()
        {
            if (_sim == null)
                return;
            
            _sim.Tick();
            _entityViewManager.UpdateTransforms(_sim.CurrentFrame);
        }

        private void OnViewIdChanged(Frame f, OnViewIdChangedEvent evt)
        {
            _entityViewManager.ReleaseView(evt.EntityId);
            _entityViewManager.RequestView(evt.EntityId, evt.ViewId);
        }

        private void OnEntityDestroyed(Frame f, OnEntityDestroyedEvent evt)
        {
            _entityViewManager.ReleaseView(evt.EntityId);
        }
    }
}
