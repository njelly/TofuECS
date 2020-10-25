using System;
using System.Collections.Generic;
using System.Linq;
using FixMath.NET;
using Tofunaut.TofuECS.Interfaces;

namespace Tofunaut.TofuECS
{
    [Serializable]
    public unsafe class Frame
    {
        public ulong number;
        public Fix64 DeltaTime;
        private Dictionary<Type, IComponent[]> _typeToComponentArray;
        private Dictionary<Type, bool[]> _typeToComponentInUseArray;
        private Dictionary<int, Dictionary<Type, int>> _entityToComponentTypeToIndex;
        private int _entityCounter;

        public Frame(Fix64 deltaTime)
        {
            DeltaTime = deltaTime;
            number = 0;
            _typeToComponentArray = new Dictionary<Type, IComponent[]>();
            _typeToComponentInUseArray = new Dictionary<Type, bool[]>();
            _entityToComponentTypeToIndex = new Dictionary<int, Dictionary<Type, int>>();
            _entityCounter = int.MinValue;
        }

        public Frame(Frame previousFrame)
        {
            number = previousFrame.number + 1;
            DeltaTime = previousFrame.DeltaTime;
            _typeToComponentArray = new Dictionary<Type, IComponent[]>(previousFrame._typeToComponentArray);
            _typeToComponentInUseArray = new Dictionary<Type, bool[]>(previousFrame._typeToComponentInUseArray);
            _entityToComponentTypeToIndex = new Dictionary<int, Dictionary<Type, int>>(previousFrame._entityToComponentTypeToIndex);
            _entityCounter = previousFrame._entityCounter;
        }

        internal void RegisterComponent<TComponent>(int max) where TComponent : class, IComponent, new()
        {
            _typeToComponentArray.Add(typeof(TComponent), new IComponent[max]);
            _typeToComponentInUseArray.Add(typeof(TComponent), new bool[max]);
        }

        public int Create()
        {
            _entityCounter++;
            _entityToComponentTypeToIndex.Add(_entityCounter, new Dictionary<Type, int>());
            return _entityCounter;
        }

        public void Destroy(int entity)
        {
            if(!_entityToComponentTypeToIndex.TryGetValue(entity, out var typeToComponentIndex))
                throw new InvalidOperationException($"the entity {entity} does not exist");

            foreach (var componentType in typeToComponentIndex.Keys)
                _typeToComponentInUseArray[componentType][typeToComponentIndex[componentType]] = false;
            
            _entityToComponentTypeToIndex.Remove(entity);
        }

        public Filter<TComponent> Filter<TComponent>() where TComponent : class, IComponent, new()
        {
            var entitiesWithComponent = _entityToComponentTypeToIndex.Keys.Where(entity => _entityToComponentTypeToIndex[entity].ContainsKey(typeof(TComponent))).ToArray();
            return new Filter<TComponent>(this, entitiesWithComponent);
        }

        public bool TryGetComponent<TComponent>(int entity, out TComponent component) where TComponent : class, IComponent, new()
        {
            component = null;
            
            if (!_entityToComponentTypeToIndex.TryGetValue(entity, out var typeToIndex) ||
                !_typeToComponentArray.TryGetValue(typeof(TComponent), out var componentArray) ||
                !typeToIndex.TryGetValue(typeof(TComponent), out var index)) 
                return false;
            
            component = (TComponent)componentArray[index];
            return true;
        }

        public TComponent AddComponent<TComponent>(int entity) where TComponent : class, IComponent, new()
        {
            // find the index of the component that is not in use
            if(!_typeToComponentInUseArray.TryGetValue(typeof(TComponent), out var inUseArray))
                throw new InvalidOperationException(
                    $"the type {typeof(TComponent).FullName} has not been registered and cannot be added to the entity");
            var i = 0;
            for(; i < inUseArray.Length; i++)
                if (!inUseArray[i])
                    break;
            if (i >= inUseArray.Length)
                throw new InvalidOperationException(
                    $"there are already {i} components of type {typeof(TComponent).FullName} in use");

            // make sure the entity is created
            if (!_entityToComponentTypeToIndex.TryGetValue(entity, out var typeToComponentIndex))
                throw new InvalidOperationException($"the entity {i} has not been created yet");
            if (typeToComponentIndex.ContainsKey(typeof(TComponent)))
                throw new InvalidOperationException(
                    $"the entity {i} already has a component of type {typeof(TComponent).FullName}");

            // record the entity as using the i'th component
            typeToComponentIndex.Add(typeof(TComponent), i);

            // now mark that index as in use
            inUseArray[i] = true;
            _typeToComponentInUseArray[typeof(TComponent)] = inUseArray; // is this line necessary?
            
            // make sure the component exists
            if (!_typeToComponentArray.TryGetValue(typeof(TComponent), out var componentArray))
                throw new InvalidOperationException(
                    $"the component of type {typeof(TComponent).FullName} has not been registered");
            
            // reset the component
            var newComponent = new TComponent();
            componentArray[i] = newComponent;
            return (TComponent)componentArray[i];
        }

        public void RemoveComponent<TComponent>(int entity) where TComponent : class, IComponent, new()
        {
            if (_typeToComponentInUseArray.TryGetValue(typeof(TComponent), out var inUseArray)
                && _entityToComponentTypeToIndex.TryGetValue(entity, out var typeToComponentIndex))
                inUseArray[typeToComponentIndex[typeof(TComponent)]] = false;
        }
    }
}