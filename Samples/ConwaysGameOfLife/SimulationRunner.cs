using System;
using System.Runtime.InteropServices;
using Tofunaut.TofuECS.Unity;
using Tofunaut.TofuECS.Samples.ConwaysGameOfLife.ECS;
using UnityEngine;
using UnityEngine.UI;

namespace Tofunaut.TofuECS.Samples.ConwaysGameOfLife
{
    public unsafe class SimulationRunner : MonoBehaviour
    {
        [SerializeField] private Vector2Int _worldSize;
        [SerializeField] private Slider _staticScaleSlider;
        [SerializeField] private Button _pauseButton;
        [SerializeField] private Text _pauseButtonLabel;
        [SerializeField] private Button _tickButton;
        [SerializeField] private Text _currentTickLabel;

        public int FrameNumber => _sim.CurrentFrame.Number;
        public ulong Seed { get; private set; }

        private Simulation _sim; 
        private Texture2D _tex2D;
        private bool _isPaused;
        private COGLInput _coglInput;

        private void Awake()
        {
            var spriteGo = new GameObject("Sprite", typeof(SpriteRenderer));
            var spriteRenderer = spriteGo.GetComponent<SpriteRenderer>();
            _tex2D = new Texture2D(_worldSize.x, _worldSize.y);
            _tex2D.filterMode = FilterMode.Point;
            var sprite = Sprite.Create(_tex2D, new Rect(0, 0, _worldSize.x, _worldSize.y), Vector2.zero, 16f);
            spriteRenderer.sprite = sprite;

            Reset((ulong)DateTime.Now.Ticks);
            
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

            _coglInput = new COGLInput();
        }

        private void Update()
        {
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

        public void Reset(ulong seed)
        {
            Seed = seed;

            _sim = new Simulation(new DummySimulationConfig(Seed), new UnityLogService(),
                new ISystem[] 
                {
                    new BoardSystem((ulong)seed, _worldSize.x, _worldSize.y)
                });
            
            // Register components BEFORE initializing the simulation!
            _sim.RegisterComponent<Board>();
            _sim.Subscribe<BoardStateChangedEvent>(OnStateChange);
            _sim.Initialize();
            
            // initialize the texture
            for(var x = 0; x < _worldSize.x; x++)
            {
                for (var y = 0; y < _worldSize.y; y++)
                {
                    _tex2D.SetPixel(x, y, Color.black);
                }
            }
        }

        private void OnStateChange(BoardStateChangedEvent evt)
        {
            for (var i = 0; i < evt.Length; i++)
                _tex2D.SetPixel(evt.XPos[i], evt.YPos[i], evt.Value[i] ? Color.white : Color.black);
            
            _tex2D.Apply();
        }

        private class DummySimulationConfig : ISimulationConfig
        {
            public int FramesInMemory => 2;
            public TData GetECSData<TData>(int id) where TData : unmanaged
            {
                return default;
            }

            public int NumInputs => 1;
            public ulong Seed { get; }

            public DummySimulationConfig(ulong seed)
            {
                Seed = seed;
            }
        }
    }
}