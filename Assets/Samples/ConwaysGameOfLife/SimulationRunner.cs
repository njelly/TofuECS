using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Tofunaut.TofuECS.Samples.ConwaysGameOfLife
{
    public unsafe class SimulationRunner : MonoBehaviour
    {
        [SerializeField] private Vector2Int _worldSize;

        public int FrameNumber => _sim.CurrentFrame.Number;
        public int Seed { get; private set; }

        private Simulation _sim;
        private Entity _boardEntity;
        private Texture2D _tex2D;

        private void Awake()
        {
            var spriteGo = new GameObject("Sprite", typeof(SpriteRenderer));
            var spriteRenderer = spriteGo.GetComponent<SpriteRenderer>();
            _tex2D = new Texture2D(_worldSize.x, _worldSize.y);
            _tex2D.filterMode = FilterMode.Point;
            var sprite = Sprite.Create(_tex2D, new Rect(0, 0, _worldSize.x, _worldSize.y), Vector2.zero, 16f);
            spriteRenderer.sprite = sprite;

            Reset((int)DateTime.Now.Ticks);
        }

        public void Reset(int seed)
        {
            if(_sim != null)
            {
                _sim.CurrentFrame.GetComponent<Board>(_boardEntity).Dispose();
            }

            _sim = new Simulation(new DummySimulationConfig(), new [] 
            {
                new BoardSystem()
            });
            _sim.RegisterComponent<Board>();
            _boardEntity = _sim.CreateEntity();
            _sim.CurrentFrame.AddComponent<Board>(_boardEntity);

            Seed = seed;
            var r = new System.Random(Seed);

            var board = _sim.CurrentFrame.GetComponentUnsafe<Board>(_boardEntity);
            board->Init(_worldSize.x, _worldSize.y);
            Board.OnSetCellValue += Board_OnSetCellValue;

            var perlinOffset = new Vector2((float)r.NextDouble() * 9999f, (float)r.NextDouble() * 9999f);
            var perlinScale = 0.01f;

            for (var x = 0; x < _worldSize.x; x++)
            {
                for (var y = 0; y < _worldSize.y; y++)
                {
                    var perlinCoord = new Vector2(x * perlinScale, y * perlinScale) + perlinOffset;
                    SetCellValue(x, y, r.NextDouble() > Mathf.PerlinNoise(perlinCoord.x, perlinCoord.y) * 0.5f);
                }
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

        public void SetCellValue(int x, int y, bool v)
        {
            var board = _sim.CurrentFrame.GetComponentUnsafe<Board>(_boardEntity);
            board->SetCellValue(x, y, v);
        }

        public void DoTick()
        {
            _sim.Tick();
            _tex2D.Apply();
        }

        public bool GetCellValue(int x, int y)
        {
            var board = _sim.CurrentFrame.GetComponent<Board>(_boardEntity);
            return board.GetState(x, y);
        }

        private class DummySimulationConfig : ISimulationConfig
        {
            public int MaxRollback => 60;

            public SimulationMode Mode => SimulationMode.Offline;

            public TAsset GetAsset<TAsset>(int id)
            {
                return default;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Board
        {
            public static event EventHandler<(Vector2Int, bool)> OnSetCellValue;

            public int Width;
            public int Height;
            public bool* State;

            public void Init(int width, int height)
            {
                Dispose();

                Width = width;
                Height = height;
                State = (bool*)Marshal.AllocHGlobal(Marshal.SizeOf(typeof(bool)) * Width * Height);
            }

            public void Dispose()
            {
                if(State != null)
                    Marshal.FreeHGlobal((IntPtr)State);
            }

            public bool GetState(int x, int y) => State[y * Width + x];

            public void SetCellValue(int x, int y, bool v)
            {
                State[y * Width + x] = v;
                OnSetCellValue.Invoke(this, (new Vector2Int(x, y), v));
            }
        }

        private class BoardSystem : ISystem
        {
            public void Process(Frame f)
            {
                var iter = f.GetIterator<Board>();
                while(iter.NextUnsafe(out var e, out var board))
                {
                    var toFlip = new List<Vector2Int>();
                    for(var x = 0; x < board->Width; x++)
                    {
                        for(var y = 0; y < board->Height; y++)
                        {
                            var currentValue = board->GetState(x, y);
                            var numAlive = 0;

                            if (board->GetState(BetterMod(x - 1, board->Width), BetterMod(y + 1, board->Height)))
                                numAlive++;
                            if (board->GetState(x, BetterMod(y + 1, board->Height)))
                                numAlive++;
                            if (board->GetState(BetterMod(x + 1, board->Width), BetterMod(y + 1, board->Height)))
                                numAlive++;
                            if (board->GetState(BetterMod(x - 1, board->Width), y))
                                numAlive++;
                            if (board->GetState(BetterMod(x + 1, board->Width), y))
                                numAlive++;
                            if (board->GetState(BetterMod(x - 1, board->Width), BetterMod(y - 1, board->Height)))
                                numAlive++;
                            if (board->GetState(x, BetterMod(y - 1, board->Height)))
                                numAlive++;
                            if (board->GetState(BetterMod(x + 1, board->Width), BetterMod(y - 1, board->Height)))
                                numAlive++;

                            if (currentValue && numAlive <= 1)
                                toFlip.Add(new Vector2Int(x, y));
                            else if (!currentValue && numAlive == 3)
                                toFlip.Add(new Vector2Int(x, y));
                            else if (currentValue && numAlive >= 4)
                                toFlip.Add(new Vector2Int(x, y));
                        }
                    }

                    foreach (var coord in toFlip)
                    {
                        var newValue = !board->GetState(coord.x, coord.y);
                        board->SetCellValue(coord.x, coord.y, newValue);
                    }
                }
            }

            public static int BetterMod(int x, int m)
            {
                var r = x % m;
                return r < 0 ? r + m : r;
            }
        }
    }
}