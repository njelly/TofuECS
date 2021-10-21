namespace Tofunaut.TofuECS.Math
{
    public class FixCircle
    {
        public FixVector2 Center;
        public Fix64 Radius;

        public Fix64 Area => Fix64.Pi * Radius * Radius;

        public FixCircle(FixVector2 center, Fix64 radius)
        {
            Center = center;
            Radius = radius;
        }

        public bool Intersects(FixCircle other) =>
            (other.Center - Center).SqrMagnitude < Fix64.Pow(Radius + other.Radius, new Fix64(2));
    }
}