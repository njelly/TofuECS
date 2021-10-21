namespace Tofunaut.TofuECS.Math
{
    public struct FixAABB
    {
        public FixVector2 Min;
        public FixVector2 Max;

        public Fix64 Width => Max.X - Min.X;
        public Fix64 Height => Max.Y - Min.Y;
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
        
        public bool Intersects(FixAABB other)
        {
            if (Max.X < other.Min.X || Min.X > other.Max.X) return false;
            if (Max.Y < other.Min.Y || Min.Y > other.Max.Y) return false;
            return true;
        }
    }
}