using Tofunaut.TofuECS.Math;
using Tofunaut.TofuECS.Physics;

namespace Tofunaut.TofuECS.Samples.Breakout.ECS
{
    public unsafe class BallSystem : ISystem
    {
        public void Initialize(Frame f) { }

        public void Process(Frame f)
        {
            var ballIterator = f.GetIterator<Ball>();
            while (ballIterator.NextUnsafe(out var ballEntityId, out var ball))
            {
                var ballTransform = f.GetComponentUnsafe<Transform2D>(ballEntityId);
                ballTransform->Position += ball->Velocity;
                
                CheckCollisionWithBricks(f, ballEntityId, ball, ballTransform);
                CheckCollisionWithWalls(f, ballEntityId, ball, ballTransform);
            }
        }

        public void Dispose(Frame f) { }

        private static void CheckCollisionWithWalls(Frame f, int ballEntityId, Ball* ball, Transform2D* ballTransform)
        {
            var config = (IBreakoutSimulationConfig)f.Config;
            
            // bounce off board right
            if (ballTransform->Position.X + ball->Radius > config.BoardMax.X)
            {
                ballTransform->Position.X = config.BoardMax.X - ball->Radius;
                ball->Velocity.X *= new Fix64(-1);
            }
            // bounce off board left
            else if (ballTransform->Position.X - ball->Radius < config.BoardMin.X)
            {
                ballTransform->Position.X = config.BoardMin.X + ball->Radius;
                ball->Velocity.X *= new Fix64(-1);
            }
            
            // bounce off board top
            if (ballTransform->Position.Y + ball->Radius > config.BoardMax.Y)
            {
                ballTransform->Position.Y = config.BoardMax.Y - ball->Radius;
                ball->Velocity.Y *= new Fix64(-1);
            }
            
            // check if bounced off the bottom or lost
            else if (ballTransform->Position.Y - ball->Radius < config.BoardMin.Y)
            {
                var player = f.GetComponentUnsafe<Player>(ball->PlayerEntityId);
                var paddle = f.GetComponent<Paddle>(player->PaddleEntityId);
                var paddleTransform = f.GetComponent<Transform2D>(player->PaddleEntityId);

                // bounce off the bottom
                if (ballTransform->Position.X >= paddleTransform.Position.X - paddle.HalfWidth &&
                    ballTransform->Position.X <= paddleTransform.Position.X + paddle.HalfWidth)
                {
                    ballTransform->Position.Y = config.BoardMin.Y + ball->Radius;
                    ball->Velocity.Y *= new Fix64(-1);
                }
                else
                {
                    player->Lives--;
                    player->RespawnTimer = config.PlayerConfig.RespawnInterval;
                    f.DestroyEntity(ballEntityId);
                }
            }
        }

        private static void CheckCollisionWithBricks(Frame f, int ballEntityId, Ball* ball, Transform2D* ballTransform)
        {
            var brickIterator = f.GetIterator<Brick>();
            while (brickIterator.Next(out var brickEntityId, out var brick))
            {
                var brickTransform = f.GetComponent<Transform2D>(brickEntityId);
                var brickAABB = new FixAABB(brickTransform.Position - brick.Extents,
                    brickTransform.Position + brick.Extents);
            }
        }
    }
}