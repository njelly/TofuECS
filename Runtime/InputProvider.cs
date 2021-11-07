using System;
using System.Collections.Concurrent;

namespace Tofunaut.TofuECS
{
    internal class InputProvider
    {
        private ConcurrentDictionary<Type, object[]> _typeToInput;

        public InputProvider()
        {
            _typeToInput = new ConcurrentDictionary<Type, object[]>();
        }
        
        public void InjectInput<TInput>(TInput[] input) where TInput : unmanaged
        {
            if (!_typeToInput.TryGetValue(typeof(TInput), out var inputArray))
            {
                inputArray = new object[input.Length];
                _typeToInput.AddOrUpdate(typeof(TInput), inputArray, (key, oldValue) => inputArray);
            }

            for (var i = 0; i < inputArray.Length; i++)
                inputArray[i] = input[i];
        }

        public TInput GetInput<TInput>(int index) where TInput : unmanaged
        {
            if(!_typeToInput.TryGetValue(typeof(TInput), out var inputArray))
                return default;

            return (TInput) inputArray[index];
        }
    }
}