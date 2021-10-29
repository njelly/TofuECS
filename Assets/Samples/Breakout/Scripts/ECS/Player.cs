using System;
using Tofunaut.TofuECS.Math;

namespace Tofunaut.TofuECS.Samples.Breakout.ECS
{
    public struct Player
    {
        public int Lives;
        public int Score;
        public int Index;
        public Fix64 RespawnTimer;
        public int PaddleEntityId;
        public int BallEntityId;

        public void Initialize(PlayerConfig config)
        {
            Lives = config.InitialLives;
            RespawnTimer = config.RespawnInterval;
            Index = config.Index;
        }
    }

    [Serializable]
    public struct PlayerConfig
    {
        public int InitialLives;
        public Fix64 RespawnInterval;
        public int Index;
    }
}