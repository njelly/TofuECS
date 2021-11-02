using Tofunaut.TofuECS.Math;

namespace Tofunaut.TofuECS.Math
{
    public interface IShape2D
    {
        FixAABB BoundingBox { get; }
        bool Contains(FixVector2 point);
        bool Intersects(IShape2D other);
        FixVector2 CollisionNormal(FixVector2 point);
    }
}