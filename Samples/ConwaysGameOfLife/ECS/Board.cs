using System;
using System.Runtime.InteropServices;
using Tofunaut.TofuECS.Utilities;

namespace Tofunaut.TofuECS.Samples.ConwaysGameOfLife.ECS
{
    public struct Board : IDisposable
    {
        public int Width;
        public int Height;
        public int Size;
        public float StartStaticThreshold;
        public UnmanagedArray<bool> State;

        public void Init(BoardConfig config)
        {
            Dispose();

            Size = config.Width * config.Height;
            Width = config.Width;
            Height = config.Height;
            StartStaticThreshold = config.StartStaticThreshold;
            State = new UnmanagedArray<bool>(Width * Height);
            
            for (var i = 0; i < Size; i++)
                State[i] = false;
        }

        public void Dispose()
        {
            State.Dispose();
        }
    }
}