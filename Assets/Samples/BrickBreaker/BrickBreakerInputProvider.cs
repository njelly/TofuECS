using UnityEngine;

namespace Tofunaut.TofuECS.Samples.BrickBreaker
{
    public class BrickBreakerInputProvider : InputProvider
    {
        public BrickBreakerInput _input;

        public BrickBreakerInputProvider()
        {
            _input = new BrickBreakerInput();
        }
        
        public override Input GetInput(int index)
        {
            _input.Direction = Vector2.zero;
            
            if(UnityEngine.Input.GetKey(KeyCode.A) || UnityEngine.Input.GetKey(KeyCode.LeftArrow))
                _input.Direction += Vector2.left;
            
            if(UnityEngine.Input.GetKey(KeyCode.D) || UnityEngine.Input.GetKey(KeyCode.RightArrow))
                _input.Direction += Vector2.right;
            
            return _input;
        }
    }
}