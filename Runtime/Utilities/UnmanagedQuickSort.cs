using System;

namespace Tofunaut.TofuECS.Utilities
{
    public static unsafe class UnmanagedQuickSort
    {
        //public static void Sort<T>(T* arr, int length, Comparison<T> comp) where T : unmanaged
        //{
        //    // bubble sort because this isn't working...
        //    for (var i = 0; i < length; i++)
        //    {
        //        for (var j = 0; j < length; j++)
        //        {
        //            if(i == j)
        //                continue;
//
        //            if (comp(arr[i], arr[j]) < 0)
        //            {
        //                Swap(arr, i, j);
        //            }
        //        }
        //    }
        //}

        public static void Sort<T>(T* arr, int length, Comparison<T> comp) where T : unmanaged =>
            SortInternal(arr, 0, length - 1, comp);

        private static void SortInternal<T>(T* arr, int left, int right, Comparison<T> comp) where T : unmanaged 
        {
            if (left < right)
            {
                /* pi is partitioning index, arr[pi] is now
                   at right place */
                var p = Partition(arr, left, right, comp);

                SortInternal(arr, left, p - 1, comp);  // Before pi
                SortInternal(arr, p + 1, right, comp); // After pi
            }
        }

        private static void Swap<T>(T* arr, int a, int b) where T : unmanaged
        {
            (arr[a], arr[b]) = (arr[b], arr[a]);
        }  

        private static int Partition<T>(T* arr, int left, int right, Comparison<T> comp) where T : unmanaged
        {
            // pivot (Element to be placed at right position)
            var pValue = arr[right];
            var i = left - 1;  // Index of smaller element and indicates the 
            // right position of pivot found so far

            for (var j = left; j <= right - 1; j++)
            {
                // If current element is smaller than the pivot
                if (comp(arr[j], pValue) < 0)
                {
                    i++;    // increment index of smaller element
                    Swap(arr, i, j);
                }
            }
            
            Swap(arr, i + 1, right);
            return i + 1;
        }
    }
}