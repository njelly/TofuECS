using System;
using System.Collections.Generic;
using FixMath.NET;
using Tofunaut.TofuECS.Interfaces;

namespace Tofunaut.TofuECS
{
    [Serializable]
    public unsafe class Frame
    {
        public ulong Number;
        public Fix64 DeltaTime;
        public Dictionary<Type, void*[]> _typeToComponentArray;

        public Frame(Fix64 deltaTime)
        {
            DeltaTime = deltaTime;
            Number = 0;
            _typeToComponentArray = new Dictionary<Type, void*[]>();
        }

        public Frame(Frame previousFrame)
        {
            Number = previousFrame.Number + 1;
            DeltaTime = previousFrame.DeltaTime;
            _typeToComponentArray = new Dictionary<Type, void*[]>(previousFrame._typeToComponentArray);
        }

        internal void RegisterComponent<TComponent>(int max) where TComponent : unmanaged, IComponent
        {
            _typeToComponentArray.Add(typeof(TComponent), new void*[max]);
        }

        public bool TryGetComponent<TComponent>(Entity e, out TComponent* component) where TComponent : unmanaged, IComponent
        {
            if(e.TryGetComponentIndex<TComponent>(out var index))
                if (_typeToComponentArray.TryGetValue(typeof(TComponent), out var componentArray))
                {
                    component = (TComponent*)componentArray[index];
                    return true;
                }

            component = null;
            return false;
        }
    }
}