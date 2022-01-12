namespace Tofunaut.TofuECS
{
    public interface IInputEventListener<TInput> where TInput : struct
    {
        void OnInputEvent(Simulation s, in TInput input);
    }
}