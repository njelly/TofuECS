using Haus.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;

namespace Tofunaut.TofuECS.Samples.ConwaysGameOfLife
{
    public unsafe class SimulationRunner : MonoBehaviour
    {
        [SerializeField] private Vector2Int _worldSize;
        [SerializeField] private Slider _staticScaleSlider;

        public int FrameNumber => _sim.CurrentFrame.Number;
        public int Seed { get; private set; }

        private Simulation _sim;
        private Entity _boardEntity;
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

            Reset((int)DateTime.Now.Ticks);
        }

        public void Reset(int seed)
        {
            if(_sim != null)
            {
                _sim.CurrentFrame.GetComponent<Board>(_boardEntity).Dispose();
            }

            Seed = seed;

            _sim = new Simulation(new DummySimulationConfig(),
                new CGOLInputProvider(_coglInput),
                new [] 
                {
                    new BoardSystem(new XorShiftRandom((ulong)seed))
                });
            _sim.RegisterComponent<Board>();
            _boardEntity = _sim.CreateEntity();
            _sim.CurrentFrame.AddComponent<Board>(_boardEntity);

            var board = _sim.CurrentFrame.GetComponentUnsafe<Board>(_boardEntity);
            board->StartStaticThreshold = 0.002f;
            board->Init(_worldSize.x, _worldSize.y);
            BoardSystem.OnSetCellValue += Board_OnSetCellValue;

            for (var i = 0; i < _worldSize.x * _worldSize.y; i++)
            {
                board->State[i] = false;
                _tex2D.SetPixel(i % board->Width, i / board->Width, Color.black);
            }

            _tex2D.Apply();
        }

        private void Board_OnSetCellValue(object sender, (Vector2Int, bool) e)
        {
            _tex2D.SetPixel(e.Item1.x, e.Item1.y, e.Item2 ? Color.white : Color.black);
        }

        private void OnDestroy()
        {
            var board = _sim.CurrentFrame.GetComponent<Board>(_boardEntity);
            board.Dispose();
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

            public SimulationMode Mode => SimulationMode.Offline;

            public int NumInputs => 1;

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

            public BoardSystem(XorShiftRandom r)
            {
                _r = r;
            }

            public void Process(Frame f)
            {
                var iter = f.GetIterator<Board>();
                var input = f.GetInput<COGLInput>(0);
                while(iter.NextUnsafe(out var e, out var board))
                {
                    var staticThreshold = board->StartStaticThreshold * input.StaticScaler;
                    var toFlip = new List<int>();
                    
                    // need to shift i so modulo operator will work as intended
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
        }
    }
}