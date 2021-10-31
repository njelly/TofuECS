using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Tofunaut.TofuECS.Math;
using Tofunaut.TofuECS.Unity;
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
                StaticScaler = Fix64.Zero,
            };

            Reset((ulong)DateTime.Now.Ticks);
        }

        private void Update()
        {
            _coglInput.StaticScaler = Fix64.FROM_FLOAT_UNSAFE(_staticScaleSlider.value);
            _sim.Tick();
            _sim.PollEvents();
            _tex2D.Apply();
        }

        public void Reset(ulong seed)
        {
            Seed = seed;

            _sim = new Simulation(new DummySimulationConfig(Seed), new UnityLogService(),
                new CGOLInputProvider(_coglInput),
                new ISystem[] 
                {
                    new BoardSystem((ulong)seed, _worldSize.x, _worldSize.y)
                });
            
            // Register components BEFORE initializing the simulation!
            _sim.RegisterComponent<Board>();
            _sim.Subscribe<StateChangeEvent>(OnStateChange);
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

        private void OnStateChange(StateChangeEvent evt)
        {
            for (var i = 0; i < evt.Length; i++)
                _tex2D.SetPixel(evt.XPos[i], evt.YPos[i], evt.Value[i] ? Color.white : Color.black);
        }

        private struct StateChangeEvent : IDisposable
        {
            public int Length;
            public int* XPos;
            public int* YPos;
            public bool* Value;

            public void Dispose()
            {
                if(XPos != null)
                    Marshal.FreeHGlobal((IntPtr)XPos);
                
                if(YPos != null)
                    Marshal.FreeHGlobal((IntPtr)YPos);
                
                if(Value != null)
                    Marshal.FreeHGlobal((IntPtr)Value);
            }
        }

        private class DummySimulationConfig : ISimulationConfig
        {
            public int MaxRollback => 60;
            public TData GetECSData<TData>(int id) where TData : unmanaged
            {
                return default;
            }

            public SimulationMode SimulationMode => SimulationMode.Offline;
            public int NumInputs => 1;
            public ulong Seed { get; }
            public int TicksPerSecond => 60;
            public PhysicsMode PhysicsMode => PhysicsMode.None;

            public DummySimulationConfig(ulong seed)
            {
                Seed = seed;
            }
        }

        private class CGOLInputProvider : InputProvider
        {
            private readonly COGLInput _coglInput;

            public CGOLInputProvider(COGLInput coglInput)
            {
                _coglInput = coglInput;
            }

            public override Input Poll(int index)
            {
                return _coglInput;
            }
        }

        private class COGLInput : Input
        {
            public Fix64 StaticScaler;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Board
        {
            public int Width;
            public int Height;
            public int Size;
            public bool* State;
            public Fix64 StartStaticThreshold;

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

            private readonly XorShiftRandom _r;
            private readonly int _boardWidth, _boardHeight;
            private int* _flippedIndexes;

            public BoardSystem(ulong seed, int boardWidth, int boardHeight)
            {
                _r = new XorShiftRandom(seed);
                _boardWidth = boardWidth;
                _boardHeight = boardHeight;
                _flippedIndexes = (int*)Marshal.AllocHGlobal(Marshal.SizeOf<int>() * _boardWidth * _boardHeight);
            }

            public void Initialize(Frame f)
            {
                var boardEntityId = f.CreateEntity();
                f.AddComponent<Board>(boardEntityId);
                
                var board = f.GetComponentUnsafe<Board>(boardEntityId);
                board->StartStaticThreshold = Fix64.FROM_FLOAT_UNSAFE(0.002f);
                board->Init(_boardWidth, _boardHeight);

                for (var i = 0; i < _boardWidth * _boardHeight; i++)
                {
                    board->State[i] = false;
                }
            }

            public void Process(Frame f)
            {
                var iter = f.GetIterator<Board>();
                var input = f.GetInput<COGLInput>(0);
                while(iter.NextUnsafe(out _, out var board))
                {
                    var staticThreshold = board->StartStaticThreshold * input.StaticScaler;
                    var numFlipped = 0;
                    
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

                        var didFlip = false;
                        if (currentState)
                        {
                            if (numAlive <= 1 || numAlive >= 4)
                                didFlip = true;
                        }
                        else if (numAlive == 3)
                        {
                            didFlip = true;
                        }
                        else if (_r.NextFix64ZeroOne() <= staticThreshold)
                            didFlip = true;

                        if (didFlip)
                            _flippedIndexes[numFlipped++] = i - board->Size;
                    }

                    var evt = new StateChangeEvent();
                    evt.Length = numFlipped;
                    evt.XPos = (int*)Marshal.AllocHGlobal(Marshal.SizeOf<int>() * numFlipped);
                    evt.YPos = (int*)Marshal.AllocHGlobal(Marshal.SizeOf<int>() * numFlipped);
                    evt.Value = (bool*)Marshal.AllocHGlobal(Marshal.SizeOf<bool>() * numFlipped);
                    
                    for (var i = 0; i < numFlipped; i++)
                    {
                        var index = _flippedIndexes[i];
                        evt.XPos[i] = index % board->Width;
                        evt.YPos[i] = index / board->Height;
                        evt.Value[i] = !board->State[index];
                        board->State[index] = evt.Value[i];
                    }
                    
                    f.RaiseEvent(evt);
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