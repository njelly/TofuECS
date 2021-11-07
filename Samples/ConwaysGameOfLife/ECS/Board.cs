using System;
using System.Runtime.InteropServices;

namespace Tofunaut.TofuECS.Samples.ConwaysGameOfLife.ECS
{
    public unsafe struct Board : IDisposable
    {
        public int Width;
        public int Height;
        public int Size;
        public float StartStaticThreshold;
        public IntPtr State;

        public void Init(BoardConfig config)
        {
            Dispose();

            Size = config.Width * config.Height;
            Width = config.Width;
            Height = config.Height;
            StartStaticThreshold = config.StartStaticThreshold;
            State = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(bool)) * Size);
            var statePtr = (bool*) State.ToPointer();
            
            // VERY important to initialize the value of pointers
            for (var i = 0; i < Size; i++)
                statePtr[i] = false;
        }

        public void Dispose()
        {
            if (State != default) 
                return;
            
            Marshal.FreeHGlobal(State);
            State = default;
        }
    }
}