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
        [SerializeField] private CellView _cellViewPrefab;

        private Simulation _sim;
        private CellView[,] _cellViews;
        private Entity _boardEntity;

        private void Start()
        {
            _cellViews = new CellView[_worldSize.x, _worldSize.y];

            _sim = new Simulation();
            _sim.RegisterComponent<Board>();
            var boardSystem = new BoardSystem();
            boardSystem.OnSetCellValue += BoardSystem_OnSetCellValue;
            _sim.RegisterSystem(boardSystem);
            _boardEntity = _sim.CreateEntity();
            _sim.AddComponent<Board>(_boardEntity);

            var board = _sim.GetComponentUnsafe<Board>(_boardEntity);
            board->Init(_worldSize.x, _worldSize.y);

            for(var x = 0; x < _worldSize.x; x++)
            {
                for(var y = 0; y < _worldSize.y; y++)
                {
                    CreateCell(x, y);
                }
            }

            //SetValue(10, 10, true);
            //SetValue(10, 12, true);
            //SetValue(11, 11, true);
        }

        private void BoardSystem_OnSetCellValue(object sender, (Vector2Int, bool) e)
        {
            SetValue(e.Item1.x, e.Item1.y, e.Item2);
        }

        private void OnDestroy()
        {
            var board = _sim.GetComponent<Board>(_boardEntity);
            board.Dispose();
        }

        public void SetValue(int x, int y, bool v)
        {
            var board = _sim.GetComponentUnsafe<Board>(_boardEntity);
            board->SetState(x, y, v);
            _cellViews[x, y].SetState(v);
        }

        public void DoTick() => _sim.Tick();

        public bool GetValue(int x, int y)
        {
            var board = _sim.GetComponent<Board>(_boardEntity);
            return board.GetState(x, y);
        }

        private void CreateCell(int x, int y)
        {
            var cellView = Instantiate(_cellViewPrefab);
            cellView.transform.position = new Vector3(x, y, 0);
            cellView.SetState(false);
            _cellViews[x, y] = cellView;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Board
        {
            public int Width;
            public int Height;
            public bool* State;

            public void Init(int width, int height)
            {
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

            public void SetState(int x, int y, bool v) => State[y * Width + x] = v;
        }

        private class BoardSystem : ISystem
        {
            public event EventHandler<(Vector2Int, bool)> OnSetCellValue;

            public void Process(Simulation sim)
            {
                var iter = sim.GetIterator<Board>();
                while(iter.NextUnsafe(out var e, out var board))
                {
                    var toFlip = new List<Vector2Int>();
                    for(var x = 0; x < board->Width; x++)
                    {
                        for(var y = 0; y < board->Height; y++)
                        {
                            var currentValue = board->GetState(x, y);

                            var neighbors = new[]
                            {
                                board->GetState(BetterMod(x - 1, board->Width), BetterMod(y + 1, board->Height)),
                                board->GetState(x, BetterMod(y - 1, board->Height)),
                                board->GetState(BetterMod(x + 1, board->Width), BetterMod(y + 1, board->Height)),
                                board->GetState(BetterMod(x - 1, board->Width), y),
                                board->GetState(BetterMod(x + 1, board->Width), y),
                                board->GetState(BetterMod(x - 1, board->Width), BetterMod(y - 1, board->Height)),
                                board->GetState(x, BetterMod(y - 1, board->Height)),
                                board->GetState(BetterMod(x + 1, board->Width), BetterMod(y - 1, board->Height)),
                            };

                            var numAlive = neighbors.Count(x => x);
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
                        board->SetState(coord.x, coord.y, newValue);
                        OnSetCellValue.Invoke(this, (coord, newValue));
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