using Tofunaut.TofuECS.Math;

namespace Tofunaut.TofuECS.Math
{
    public interface IShape
    {
        FixAABB BoundingBox { get; }
        bool Contains(FixVector2 point);
        bool Intersects(IShape other);
    }
}