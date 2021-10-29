using Tofunaut.TofuECS.Math;

namespace Tofunaut.TofuECS.Math
{
    internal interface IShape
    {
        FixAABB BoundingBox { get; }
    }
}