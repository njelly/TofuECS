using Tofunaut.TofuECS.Math;
using UnityEngine;

namespace Tofunaut.TofuECS.Samples.BrickBreaker
{
    public struct Ball
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public float Radius;
        public bool StuckToPaddle;
        public int PaddleEntityId;

        public Fix64 Test;
    }
}