using System;

namespace Tofunaut.TofuECS.Utilities
{
    public static unsafe class UnmanagedQuickSort
    {
        public static void Sort<T>(T* arr, int length, Comparison<T> comp) where T : unmanaged =>
            SortInternal(arr, 0, length - 1, comp);

        private static void SortInternal<T>(T* arr, int left, int right, Comparison<T> comp) where T : unmanaged
        {
            if (left >= right) 
                return;
            
            var p = Partition(arr, left, right, comp);
            SortInternal(arr, left, p - 1, comp);
            SortInternal(arr, p + 1, right, comp);
        }

        private static void Swap<T>(T* arr, int a, int b) where T : unmanaged
        {
            (arr[a], arr[b]) = (arr[b], arr[a]);
        }  

        private static int Partition<T>(T* arr, int left, int right, Comparison<T> comp) where T : unmanaged
        {
            var pValue = arr[right];
            var i = left - 1;
            for (var j = left; j <= right - 1; j++)
            {
                if (comp(arr[j], pValue) >= 0) 
                    continue;
                
                i++;
                Swap(arr, i, j);
            }
            
            Swap(arr, i + 1, right);
            return i + 1;
        }
    }
}