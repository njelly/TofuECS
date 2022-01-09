using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Tofunaut.TofuECS
{
    public class ECSDatabase
    {
        private IDictionary<int, object> _idToData;
        private IDictionary<Type, object> _typeToSingletonData;

        public ECSDatabase()
        {
            _idToData = new Dictionary<int, object>();
            _typeToSingletonData = new Dictionary<Type, object>();
        }

        public void Seal()
        {
            _idToData = new ReadOnlyDictionary<int, object>(_idToData);
            _typeToSingletonData = new ReadOnlyDictionary<Type, object>(_typeToSingletonData);
        }

        public void RegisterById<TData>(int id, TData data) where TData : struct => _idToData[id] = data;

        public void RegisterSingleton<TData>(TData data) where TData : struct =>
            _typeToSingletonData[typeof(TData)] = data;

        public void GetById<TData>(int id, out TData data) => data = (TData) _idToData[id];

        public void GetSingleton<TData>(out TData data) => data = (TData) _typeToSingletonData[typeof(TData)];
    }
}