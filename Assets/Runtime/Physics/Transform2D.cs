using Tofunaut.TofuECS.Math;

namespace Tofunaut.TofuECS.Physics
{
    public struct Transform2D
    {
        public FixVector2 Position;
        internal FixVector2 PrevPosition;
        public Fix64 Rotation;
    }
}