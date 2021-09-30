using System;
using System.Collections.Generic;

namespace Tofunaut.TofuECS
{
    public class Entity
    {
        public int Id { get; }
        public bool IsDestroyed { get; private set; }
        internal int this[Type type] => _typeToComponentIndexes[type];
        
        internal IReadOnlyDictionary<Type, int> TypeToComponentIndexes => _typeToComponentIndexes;

        private readonly Dictionary<Type, int> _typeToComponentIndexes;

        public Entity(int id)
        {
            Id = id;
            _typeToComponentIndexes = new Dictionary<Type, int>();
        }

        internal void Destroy() => IsDestroyed = true;

        internal void AssignComponent(Type type, int index) => _typeToComponentIndexes[type] = index;

        internal void UnassignComponent<TComponent>(Type type) where TComponent : unmanaged =>
            _typeToComponentIndexes.Remove(typeof(TComponent));
        
        internal bool TryGetComponentIndex<TComponent>(out int index) where TComponent : unmanaged =>
            _typeToComponentIndexes.TryGetValue(typeof(TComponent), out index);

    }
}