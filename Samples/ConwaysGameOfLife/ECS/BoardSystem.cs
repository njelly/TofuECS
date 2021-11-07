using System;
using System.Runtime.InteropServices;
using Tofunaut.TofuECS.Utilities;

namespace Tofunaut.TofuECS.Samples.ConwaysGameOfLife.ECS
{
    public unsafe class BoardSystem : ISystem
    {
        private int* _flippedIndexes;

        public void Initialize(Frame f)
        {
            var boardEntityId = f.CreateEntity();
            f.AddComponent<Board>(boardEntityId);
            f.AddComponent<ViewId>(boardEntityId);
            
            var board = f.GetComponentUnsafe<Board>(boardEntityId);
            var boardConfig = f.Config.GetECSData<BoardConfig>(((ICOGLSimulationConfig) f.Config).BoardConfigId);
            board->Init(boardConfig);
            _flippedIndexes = (int*)Marshal.AllocHGlobal(Marshal.SizeOf<int>() * boardConfig.Width * boardConfig.Height);

            var viewId = f.GetComponentUnsafe<ViewId>(boardEntityId);
            viewId->Id = boardConfig.ViewId;
        }

        public void Process(Frame f)
        {
            var iter = f.GetIterator<Board>();
            var input = f.GetInput<COGLInput>(0);
            while(iter.NextUnsafe(out _, out var board))
            {
                var staticThreshold = board->StartStaticThreshold * input.StaticScale;
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
                    else if (f.RNG.NextDouble() <= staticThreshold)
                        didFlip = true;

                    if (didFlip)
                        _flippedIndexes[numFlipped++] = i - board->Size;
                }

                var evt = new BoardStateChangedEvent();
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
            
            Marshal.FreeHGlobal((IntPtr)_flippedIndexes);
        }
    }
}