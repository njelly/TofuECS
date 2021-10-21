using Tofunaut.TofuECS.Math;

namespace Tofunaut.TofuECS.Physics
{
    public unsafe struct DynamicBody2D
    {
        public FixVector2 Velocity;
        public Fix64 AngularVelocity;
        public Collider Collider;
        public bool IsAsleep;
        internal FixVector2* Forces;
        internal int ForcesNextIndex;
        internal int ForcesLength;

        public void AddForce(FixVector2 force)
        {
            
        }
    }

    public enum ShapeType
    {
        None,
        AABB,
        Circle,
    }

    public struct Collider
    {
        public ShapeType ShapeType;
        public Fix64 CircleRadius;
        public FixVector2 BoxExtents;
        public FixAABB BoundingBox;
    }
}