using System;
using System.Runtime.InteropServices;

namespace Tofunaut.TofuECS.Samples.ConwaysGameOfLife.ECS
{
    public unsafe struct Board
    {
        public int Width;
        public int Height;
        public int Size;
        public float StartStaticThreshold;
        public bool* State;

        public void Init(BoardConfig config)
        {
            Dispose();

            Size = config.Width * config.Height;
            Width = config.Width;
            Height = config.Height;
            StartStaticThreshold = config.StartStaticThreshold;
            State = (bool*)Marshal.AllocHGlobal(Marshal.SizeOf(typeof(bool)) * Size);
        }

        public void Dispose()
        {
            if(State != null)
                Marshal.FreeHGlobal((IntPtr)State);
        }
    }
}