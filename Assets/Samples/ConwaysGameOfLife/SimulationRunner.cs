using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Tofunaut.TofuECS.Math;
using UnityEngine;
using UnityEngine.UI;

namespace Tofunaut.TofuECS.Samples.ConwaysGameOfLife
{
    public unsafe class SimulationRunner : MonoBehaviour
    {
        [SerializeField] private Vector2Int _worldSize;
        [SerializeField] private Slider _staticScaleSlider;

        public int FrameNumber => _sim.CurrentFrame.Number;
        public ulong Seed { get; private set; }

        private Simulation _sim; 
        private Texture2D _tex2D;
        private COGLInput _coglInput;

        private void Awake()
        {
            var spriteGo = new GameObject("Sprite", typeof(SpriteRenderer));
            var spriteRenderer = spriteGo.GetComponent<SpriteRenderer>();
            _tex2D = new Texture2D(_worldSize.x, _worldSize.y);
            _tex2D.filterMode = FilterMode.Point;
            var sprite = Sprite.Create(_tex2D, new Rect(0, 0, _worldSize.x, _worldSize.y), Vector2.zero, 16f);
            spriteRenderer.sprite = sprite;

            _coglInput = new COGLInput
            {
                StaticScaler = 0f,
            };

            Reset((ulong)DateTime.Now.Ticks);
        }

        public void Reset(ulong seed)
        {
            Seed = seed;
            
            BoardSystem.OnSetCellValue += Board_OnSetCellValue;
            
            _sim = new Simulation(new DummySimulationConfig(Seed),
                new CGOLInputProvider(_coglInput),
                new ISystem[] 
                {
                    new BoardSystem((ulong)seed, _worldSize.x, _worldSize.y)
                });
            
            // Register components BEFORE initializing the simulation!
            _sim.RegisterComponent<Board>();
            _sim.Initialize();

            _tex2D.Apply();
        }

        private void Board_OnSetCellValue(object sender, (Vector2Int, bool) e)
        {
            _tex2D.SetPixel(e.Item1.x, e.Item1.y, e.Item2 ? Color.white : Color.black);
        }

        public void DoTick()
        {
            _coglInput.StaticScaler = _staticScaleSlider.value;

            _sim.Tick();
            _tex2D.Apply();
        }

        private class DummySimulationConfig : ISimulationConfig
        {
            public int MaxRollback => 60;
            public SimulationMode SimulationMode => SimulationMode.Offline;
            public int NumInputs => 1;
            public ulong Seed { get; }
            public Fix64 DeltaTime => new Fix64(1) / new Fix64(30);
            public PhysicsMode PhysicsMode => PhysicsMode.None;

            public DummySimulationConfig(ulong seed)
            {
                Seed = seed;
            }

            public TAsset GetAsset<TAsset>(int id)
            {
                return default;
            }
        }

        private class CGOLInputProvider : InputProvider
        {
            private readonly COGLInput _coglInput;

            public CGOLInputProvider(COGLInput coglInput)
            {
                _coglInput = coglInput;
            }

            public override Input GetInput(int index)
            {
                return _coglInput;
            }
        }

        private class COGLInput : Input
        {
            public float StaticScaler;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Board
        {
            public int Width;
            public int Height;
            public int Size;
            public bool* State;
            public float StartStaticThreshold;

            public void Init(int width, int height)
            {
                Dispose();

                Size = width * height;
                Width = width;
                Height = height;
                State = (bool*)Marshal.AllocHGlobal(Marshal.SizeOf(typeof(bool)) * Size);
            }

            public void Dispose()
            {
                if(State != null)
                    Marshal.FreeHGlobal((IntPtr)State);
            }
        }

        private class BoardSystem : ISystem
        {
            public static event EventHandler<(Vector2Int, bool)> OnSetCellValue;

            private readonly XorShiftRandom _r;
            private readonly int _boardWidth, _boardHeight;

            public BoardSystem(ulong seed, int boardWidth, int boardHeight)
            {
                _r = new XorShiftRandom(seed);
                _boardWidth = boardWidth;
                _boardHeight = boardHeight;
            }

            public void Initialize(Frame f)
            {
                var boardEntityId = f.CreateEntity();
                f.AddComponent<Board>(boardEntityId);
                
                var board = f.GetComponentUnsafe<Board>(boardEntityId);
                board->StartStaticThreshold = 0.002f;
                board->Init(_boardWidth, _boardHeight);

                for (var i = 0; i < _boardWidth * _boardHeight; i++)
                {
                    board->State[i] = false;
                    OnSetCellValue?.Invoke(this,  (new Vector2Int(i % _boardWidth, i / _boardHeight), false));
                }
            }

            public void Process(Frame f)
            {
                var iter = f.GetIterator<Board>();
                var input = f.GetInput<COGLInput>(0);
                while(iter.NextUnsafe(out _, out var board))
                {
                    var staticThreshold = board->StartStaticThreshold * input.StaticScaler;
                    var toFlip = new List<int>();
                    
                    // need to add the board->Size to i so modulo operator will work as intended
                    for (var i = board->Size; i < board->Size * 2; i++)
                    {
                        var numAlive = 0;
                        var currentState = board->State[i % board->Size];
                        
                        // TOP-LEFT
                        if (board->State[(i - 1 + board->Width) % board->Size])
                            numAlive++;
                        
                        // TOP-CENTER
                        if (board->State[(i + board->Width) % board->Size])
                            numAlive++;
                        
                        // TOP-RIGHT
                        if (board->State[(i + 1 + board->Width) % board->Size])
                            numAlive++;
                        
                        // MIDDLE-LEFT
                        if (board->State[(i - 1) % board->Size])
                            numAlive++;
                        
                        // MIDDLE-RIGHT
                        if (board->State[(i + 1) % board->Size])
                            numAlive++;
                        
                        // BOTTOM-LEFT
                        if (board->State[(i - 1 - board->Width) % board->Size])
                            numAlive++;
                        
                        // BOTTOM-CENTER
                        if (board->State[(i - board->Width) % board->Size])
                            numAlive++;
                        
                        // BOTTOM-RIGHT
                        if (board->State[(i + 1 - board->Width) % board->Size])
                            numAlive++;
                        
                        if (currentState && numAlive <= 1)
                            toFlip.Add(i);
                        else if (!currentState && numAlive == 3)
                            toFlip.Add(i);
                        else if (currentState && numAlive >= 4)
                            toFlip.Add(i);
                        
                        // non-conway rules, just a test
                        else if (_r.NextDouble() <= staticThreshold)
                            toFlip.Add(i);
                    }

                    foreach (var index in toFlip)
                    {
                        var i = index - board->Size;
                        board->State[i] = !board->State[i];
                        OnSetCellValue?.Invoke(this, (new Vector2Int(i % board->Width, i / board->Height), board->State[i]));
                    }
                }
            }

            public void Dispose(Frame f)
            {
                var iter = f.GetIterator<Board>();
                while(iter.NextUnsafe(out _, out var board))
                    board->Dispose();
            }
        }
    }
}