using Tofunaut.TofuECS.Math;

namespace Tofunaut.TofuECS.Samples.Breakout.ECS
{
    public unsafe class PlayerSystem : ISystem
    {
        public void Initialize(Frame f)
        {
            // create a player for each input
            for (var i = 0; i < f.NumInputs; i++)
            {
                var playerEntity = f.CreateEntity();
                f.AddComponent<Player>(playerEntity);
                var player = f.GetComponentUnsafe<Player>(playerEntity);
                var playerConfig = ((IBreakoutSimulationConfig)f.Config).PlayerConfig;
                playerConfig.Index = i;
                player->Initialize(playerConfig);
            }
        }

        public void Process(Frame f)
        {
            var playerIterator = f.GetIterator<Player>();
            while (playerIterator.NextUnsafe(out var playerEntityId, out var player))
            {
                if (player->RespawnTimer < Fix64.Zero)
                    continue;
                
                player->RespawnTimer -= f.DeltaTime;

                if (player->RespawnTimer < Fix64.Zero)
                    PaddleSystem.SpawnPaddle(f, playerEntityId);
            }
        }

        public void Dispose(Frame f) { }
    }
}