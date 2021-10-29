using Tofunaut.TofuECS.Math;
using Tofunaut.TofuECS.Physics;
using Tofunaut.TofuECS.Utilities;

namespace Tofunaut.TofuECS.Samples.Breakout.ECS
{
    public unsafe class PaddleSystem : ISystem
    {
        public void Initialize(Frame f) { }

        public void Process(Frame f)
        {
            var config = (IBreakoutSimulationConfig)f.Config;
            var paddleIterator = f.GetIterator<Paddle>();
            while (paddleIterator.NextUnsafe(out var entityId, out var paddle))
            {
                var paddleTransform = f.GetComponentUnsafe<Transform2D>(entityId);
                var player = f.GetComponent<Player>(paddle->PlayerEntityId);
                var input = f.GetInput<BreakoutInput>(player.Index);
                paddleTransform->Position.X = Fix64.Clamp(paddleTransform->Position.X + input.PaddleDeltaX, config.BoardMin.X, config.BoardMax.X);
            }
        }

        public void Dispose(Frame f)
        {
            
        }

        public static void SpawnPaddle(Frame f, int playerEntityId)
        {
            var paddleEntityId = f.CreateEntity();
            f.AddComponent<Paddle>(paddleEntityId);
            f.AddComponent<ViewId>(paddleEntityId);
            f.AddComponent<Transform2D>(paddleEntityId);
            
            var paddle = f.GetComponentUnsafe<Paddle>(paddleEntityId);
            paddle->Initialize(f, playerEntityId);

            var viewId = f.GetComponentUnsafe<ViewId>(paddleEntityId);
            viewId->Id = ((IBreakoutSimulationConfig)f.Config).PaddleViewId;

            f.GetComponentUnsafe<Player>(playerEntityId)->PaddleEntityId = paddleEntityId;
        }
    }
}