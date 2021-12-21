using System;
using System.Runtime.InteropServices;
using Tofunaut.TofuECS.Unity;
using Tofunaut.TofuECS.Samples.ConwaysGameOfLife.ECS;
using Tofunaut.TofuECS.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace Tofunaut.TofuECS.Samples.ConwaysGameOfLife
{
    public class SimulationRunner : MonoBehaviour
    {
        public static SimulationRunner Instance { get; private set; }
        
        [SerializeField] private COGLSimulationConfigAsset _configAsset;
        [SerializeField] private ECSDatabaseAsset ecsDatabaseAsset;
        [SerializeField] private Slider _staticScaleSlider;
        [SerializeField] private Button _pauseButton;
        [SerializeField] private Text _pauseButtonLabel;
        [SerializeField] private Button _tickButton;
        [SerializeField] private Text _currentTickLabel;

        public ulong Seed { get; private set; }
        public Simulation Simulation => _sim;

        private Simulation _sim;
        private bool _isPaused;
        private COGLInput _coglInput;
        private EntityViewManager _entityViewManager;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        private async void Start()
        {
            await ecsDatabaseAsset.PreloadAll();
            _entityViewManager = new EntityViewManager(ecsDatabaseAsset);
            
            _pauseButton.onClick.RemoveAllListeners();
            _pauseButton.onClick.AddListener(() =>
            {
                _isPaused = !_isPaused;
                _pauseButtonLabel.text = _isPaused ? "Unpause" : "Pause";
            });
            
            _tickButton.onClick.RemoveAllListeners();
            _tickButton.onClick.AddListener(() =>
            {
                _sim.Tick();
                _sim.PollEvents();
            });

            _sim = new Simulation(_configAsset, ecsDatabaseAsset.BuildECSDatabase(), new UnityLogService(),
                new ISystem[]
                {
                    new ViewIdSystem(),
                    new BoardSystem()
                });
            _sim.RegisterComponent<Board>();
            _sim.RegisterComponent<ViewId>();
            _sim.Subscribe<OnViewIdChangedEvent>(OnViewIdChanged);
            _sim.Initialize();

            _coglInput = new COGLInput();
        }

        private void Update()
        {
            if (_sim == null)
                return;
            
            // inject new input ONLY when the input has changed
            if (Math.Abs(_coglInput.StaticScale - _staticScaleSlider.value) > 0.01f)
            {
                _coglInput.StaticScale = _staticScaleSlider.value;
                _sim.InjectNewInput(new []
                {
                    _coglInput
                });
            }

            _currentTickLabel.text = $"Tick: {_sim.CurrentFrame.Number}";
            
            if (_isPaused)
                return;
            
            _sim.Tick();
            _sim.PollEvents();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
            
            if(_sim != null)
                _sim.Shutdown();
        }

        private void OnViewIdChanged(OnViewIdChangedEvent evt)
        {
            _entityViewManager.ReleaseView(evt.PrevId);
            _entityViewManager.RequestView(evt.EntityId, evt.ViewId);
        }
    }
}