using Tofunaut.TofuECS.Samples.ConwaysGameOfLife.ECS;
using Tofunaut.TofuECS.Unity;
using UnityEngine;

namespace Tofunaut.TofuECS.Samples.ConwaysGameOfLife
{
    [RequireComponent(typeof(EntityView))]
    [RequireComponent(typeof(SpriteRenderer))]
    public unsafe class BoardView : MonoBehaviour
    {
        private EntityView _entityView;
        private SpriteRenderer _spriteRenderer;
        private Texture2D _texture2D;
        
        private void Awake()
        {
            _entityView = GetComponent<EntityView>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void OnInitialize()
        {
            var board = SimulationRunner.Instance.Simulation.CurrentFrame.GetComponent<Board>(_entityView.EntityId);
            _texture2D = new Texture2D(board.Width, board.Height);
            _texture2D.filterMode = FilterMode.Point;
            _spriteRenderer.sprite = Sprite.Create(_texture2D, new Rect(0, 0, board.Width, board.Height), Vector2.zero, 16f);

            for (var x = 0; x < board.Width; x++)
            {
                for (var y = 0; y < board.Height; y++)
                {
                    _texture2D.SetPixel(x, y, Color.black);
                }
            }
            
            _texture2D.Apply();
            
            SimulationRunner.Instance.Simulation.Subscribe<BoardStateChangedEvent>(OnBoardStateChanged);
        }

        public void OnCleanUp()
        {
            SimulationRunner.Instance.Simulation.Subscribe<BoardStateChangedEvent>(OnBoardStateChanged);
        }

        private void OnBoardStateChanged(BoardStateChangedEvent evt)
        {
            for (var i = 0; i < evt.Length; i++)
                _texture2D.SetPixel(evt.XPos[i], evt.YPos[i], evt.Value[i] ? Color.white : Color.black);

            _texture2D.Apply();
        }
    }
}