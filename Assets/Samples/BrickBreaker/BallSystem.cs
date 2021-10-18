using UnityEngine;

namespace Tofunaut.TofuECS.Samples.BrickBreaker
{
    public unsafe class BallSystem : ISystem
    {
        public void Initialize(Frame f) { }

        public void Process(Frame f)
        {
            var allBalls = f.GetIterator<Ball>();
            while (allBalls.NextUnsafe(out _, out var ball))
            {
                if (ball->StuckToPaddle)
                {
                    Process_StuckToPaddle(f, ball);
                    continue;
                }

                //var newPos = ball->Position + ball->Velocity * Time.deltaTime;
                //var penetrateLeftRight = -1f;
                //var penetrateTopBottom = -1;
                //
                //// do collision checks...
//
                //if (penetrateLeftRight >= 0f)
                //{
                //    var leftRightSign = Mathf.Sign(ball->Velocity.x);
                //    newPos += new Vector2(penetrateLeftRight * leftRightSign * -1, 0f);
                //    ball->Velocity = new Vector2()
                //}
            }
        }

        private static void Process_StuckToPaddle(Frame f, Ball* ball)
        {
            if (!f.IsValid(ball->PaddleEntityId))
                return;

            var paddle = f.GetComponent<Paddle>(ball->PaddleEntityId);
            ball->Position = paddle.Position + new Vector2(0f, paddle.Extents.y + ball->Radius);
        }

        public void Dispose(Frame f) { }
    }
}