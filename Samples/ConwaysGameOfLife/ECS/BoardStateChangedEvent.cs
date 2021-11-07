using System;
using System.Runtime.InteropServices;

namespace Tofunaut.TofuECS.Samples.ConwaysGameOfLife.ECS
{
    public unsafe struct BoardStateChangedEvent : IDisposable
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
}