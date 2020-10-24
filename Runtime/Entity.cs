using System;
using System.Collections.Generic;
using Tofunaut.TofuECS.Interfaces;

namespace Tofunaut.TofuECS
{
    public class Entity
    {
        private readonly int _id;
        private Dictionary<Type, int> _typeToComponentIndex;

        internal Entity(int id)
        {
            _id = id;
            _typeToComponentIndex = new Dictionary<Type, int>();
        }

        internal void AddComponent<T>(int index) where T : unmanaged, IComponent
        {
            _typeToComponentIndex.Add(typeof(T), index);
        }

        internal bool TryGetComponentIndex<T>(out int index) where T : unmanaged, IComponent
        {
            return _typeToComponentIndex.TryGetValue(typeof(T), out index);
        }

        public static implicit operator int(Entity e) => e._id;
        public static implicit operator Entity(int id) => new Entity(id);
    }
}