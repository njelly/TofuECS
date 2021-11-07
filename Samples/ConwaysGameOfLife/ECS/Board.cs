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
}