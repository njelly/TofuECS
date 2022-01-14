using System.Numerics;

namespace TofuECS.Demo.Common;

public struct SpeedComponent
{
    public int Velocity;
    public Vector2 Direction;

    public SpeedComponent(int velocity, Vector2 direction)
    {
        Velocity = velocity;
        Direction = direction;
    }
}