using System;
using System.Runtime.InteropServices;
using Tofunaut.TofuECS.Utilities;

namespace Tofunaut.TofuECS.Samples.ConwaysGameOfLife.ECS
{
    public class BoardSystem : ISystem
    {
        private UnmanagedArray<int> _flippedIndexes;

        public unsafe void Initialize(Frame f)
        {
            var boardEntityId = f.CreateEntity();
            f.AddComponent<Board>(boardEntityId);
            f.AddComponent<ViewId>(boardEntityId);
            
            var board = f.GetComponentUnsafe<Board>(boardEntityId);
            var boardConfig = f.Database.Get<BoardConfig>(((ICOGLSimulationConfig) f.Config).BoardConfigId);
            board->Init(boardConfig);
            _flippedIndexes = new UnmanagedArray<int>(board->Size);

            var viewId = f.GetComponentUnsafe<ViewId>(boardEntityId);
            viewId->Id = boardConfig.ViewId;
        }

        public unsafe void Process(Frame f)
        {
            var iter = f.GetIterator<Board>();
            var input = f.GetInput<COGLInput>(0);
            var flippedIndexesArray = _flippedIndexes.RawValue;
            while(iter.NextUnsafe(out _, out var board))
            {
                var staticThreshold = board->StartStaticThreshold * input.StaticScale;
                var numFlipped = 0;

                var rawBoardState = board->State.RawValue;
                // need to add the board->Size to i so modulo operator will work as intended
                for (var i = board->Size; i < board->Size * 2; i++)
                {
                    var numAlive = 0;
                    var currentState = rawBoardState[i % board->Size];
                    
                    // TOP-LEFT
                    if (rawBoardState[(i - 1 + board->Width) % board->Size])
                        numAlive++;
                    
                    // TOP-CENTER
                    if (rawBoardState[(i + board->Width) % board->Size])
                        numAlive++;
                    
                    // TOP-RIGHT
                    if (rawBoardState[(i + 1 + board->Width) % board->Size])
                        numAlive++;
                    
                    // MIDDLE-LEFT
                    if (rawBoardState[(i - 1) % board->Size])
                        numAlive++;
                    
                    // MIDDLE-RIGHT
                    if (rawBoardState[(i + 1) % board->Size])
                        numAlive++;
                    
                    // BOTTOM-LEFT
                    if (rawBoardState[(i - 1 - board->Width) % board->Size])
                        numAlive++;
                    
                    // BOTTOM-CENTER
                    if (rawBoardState[(i - board->Width) % board->Size])
                        numAlive++;
                    
                    // BOTTOM-RIGHT
                    if (rawBoardState[(i + 1 - board->Width) % board->Size])
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
                        flippedIndexesArray[numFlipped++] = i - board->Size;
                }

                var evt = new BoardStateChangedEvent();
                evt.Length = numFlipped;
                evt.XPos = (int*)Marshal.AllocHGlobal(Marshal.SizeOf<int>() * numFlipped);
                evt.YPos = (int*)Marshal.AllocHGlobal(Marshal.SizeOf<int>() * numFlipped);
                evt.Value = (bool*)Marshal.AllocHGlobal(Marshal.SizeOf<bool>() * numFlipped);
                
                for (var i = 0; i < numFlipped; i++)
                {
                    var index = flippedIndexesArray[i];
                    evt.XPos[i] = index % board->Width;
                    evt.YPos[i] = index / board->Height;
                    evt.Value[i] = !rawBoardState[index];
                    rawBoardState[index] = evt.Value[i];
                }
                
                f.RaiseExternalEvent(evt);
            }
        }

        public unsafe void Dispose(Frame f)
        {
            var iter = f.GetIterator<Board>();
            while(iter.NextUnsafe(out _, out var board))
                board->Dispose();

            _flippedIndexes.Dispose();
        }
    }
}