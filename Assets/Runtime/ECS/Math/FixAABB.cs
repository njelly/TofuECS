using System;

namespace Tofunaut.TofuECS.Math
{
    public struct FixAABB : IShape2D
    {
        public FixVector2 Min;
        public FixVector2 Max;

        public Fix64 Width => Max.X - Min.X;
        public Fix64 Height => Max.Y - Min.Y;
        public FixVector2 Center => (new FixVector2(Max.X - Min.X, Max.Y - Min.Y) / new Fix64(2)) + Min;
        public FixVector2 Extents => (Max - Min) / new Fix64(2);

        public FixVector2[] Points => new []
        {
            new FixVector2(Max.X, Max.Y),
            new FixVector2(Max.X, Min.Y),
            new FixVector2(Min.X, Min.Y),
            new FixVector2(Min.X, Max.Y)
        };
        public Fix64 Area => Width * Height;

        public FixAABB(FixVector2 min, FixVector2 max)
        {
            Min = min;
            Max = max;
        }

        public FixAABB(FixVector2 min, Fix64 width, Fix64 height)
        {
            Min = min;
            Max = Min + new FixVector2(width, height);
        }

        public bool Contains(FixVector2 point) =>
            point.X <= Max.X && point.X >= Min.Y && point.Y <= Max.Y && point.Y >= Min.Y;
        
        public bool IntersectsAABB(FixAABB other)
        {
            if (Max.X < other.Min.X || Min.X > other.Max.X) return false;
            if (Max.Y < other.Min.Y || Min.Y > other.Max.Y) return false;
            return true;
        }

        public bool IntersectsCircle(FixCircle circle)
        {
            // based on Jack Ding's solution: https://gamedev.stackexchange.com/questions/96337/collision-between-aabb-and-circle
            
            // up down rect
            if (new FixAABB(Min + new FixVector2(Fix64.Zero, -circle.Radius),
                Max + new FixVector2(Fix64.Zero, circle.Radius)).Contains(circle.Center))
                return true;
            
            // left right rect
            if (new FixAABB(Min + new FixVector2(-circle.Radius, Fix64.Zero),
                Max + new FixVector2(circle.Radius, Fix64.Zero)).Contains(circle.Center))
                return true;
            
            // check corners
            var points = Points;
            var cornerCircles = new FixCircle[points.Length];
            for (var i = 0; i < cornerCircles.Length; i++)
            {
                if (new FixCircle(points[i], circle.Radius).Contains(circle.Center))
                    return true;
            }

            return false;
        }

        public bool Intersects(IShape2D other)
        {
            return other switch
            {
                FixCircle otherCircle => IntersectsCircle(otherCircle),
                FixAABB otherAABB => IntersectsAABB(otherAABB),
                FixPoint otherPoint => Contains(otherPoint.Position),
                _ => throw new NotImplementedException(
                    "FixAABB.Intersects(IShape other) is not implemented for that IShape implementation")
            };
        }

        public FixVector2 CollisionNormal(FixVector2 point)
        {
            var normals = new[]
            {
                FixVector2.Up,
                FixVector2.Right,
                FixVector2.Down,
                FixVector2.Left,
            };

            var fromCenter = point - Center;
            var highestDot = Fix64.MinValue;
            var highestIndex = 0;
            for (var i = 0; i < normals.Length; i++)
            {
                var dot = FixVector2.Dot(fromCenter, normals[i]);
                if (dot <= highestDot) 
                    continue;
                
                highestDot = dot;
                highestIndex = i;
            }

            return normals[highestIndex];
        }

        public FixAABB BoundingBox => this;
    }
}