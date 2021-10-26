namespace Tofunaut.TofuECS
{
    public abstract class InputProvider
    {
        /// <summary>
        /// Called once per tick and before systems are processed.
        /// </summary>
        public abstract Input Poll(int index);
    }
}
