namespace Tofunaut.TofuECS.Math
{
    public struct FixVector2
    {
        public static FixVector2 Right => new FixVector2(Fix64.One, Fix64.Zero);
        public static FixVector2 Down => new FixVector2(Fix64.Zero, -Fix64.One);
        public static FixVector2 Left => new FixVector2(-Fix64.One, Fix64.Zero);
        public static FixVector2 Up => new FixVector2(Fix64.Zero, Fix64.One);
        public static FixVector2 Zero => new FixVector2(Fix64.Zero, Fix64.Zero);
        public static FixVector2 One => new FixVector2(Fix64.One, Fix64.One);
        
        public Fix64 X;
        public Fix64 Y;

        public Fix64 Magnitude => Fix64.Sqrt(SqrMagnitude);
        public Fix64 SqrMagnitude => X * X + Y * Y;
        public Fix64 ManhattanDistance => X + Y;
        
        public FixVector2(Fix64 x, Fix64 y)
        {
            X = x;
            Y = y;
        }

        public FixVector2 Rotate(Fix64 radians) => new FixVector2(Fix64.Cos(radians), Fix64.Sin(radians)) * Magnitude;

        public static FixVector2 operator +(FixVector2 a, FixVector2 b) => new FixVector2(a.X + b.X, a.Y + b.Y);
        public static FixVector2 operator -(FixVector2 a, FixVector2 b) => new FixVector2(a.X - b.X, a.Y - b.Y);
        public static FixVector2 operator *(FixVector2 a, Fix64 s) => new FixVector2(a.X * s, a.Y * s);
        public static FixVector2 operator /(FixVector2 a, Fix64 s) => new FixVector2(a.X / s, a.Y / s);
        public static bool operator ==(FixVector2 a, FixVector2 b) => a.X == b.X && a.Y == b.Y;
        public static bool operator !=(FixVector2 a, FixVector2 b) => a.X != b.X || a.Y != b.Y;
    }
}