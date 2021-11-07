using System;

namespace Tofunaut.TofuECS
{
    public interface IInputPoller
    {
        Type Type { get; }
        object Poll(int index);
    }
}