namespace Tofunaut.TofuECS.Utilities
{
    public static unsafe class UnmanagedQuickSort
    {
        public delegate bool Comparison<T>(T a, T b) where T : unmanaged;
        public static void Sort<T>(T* arr, int start, int end, Comparison<T> comp) where T : unmanaged
        {
            if (start >= end) 
                return;
            
            var i = Partition(arr, start, end, comp);
 
            Sort(arr, start, i - 1, comp);
            Sort(arr, i + 1, end, comp);
        }
 
        private static int Partition<T>(T* arr, int start, int end, Comparison<T> comp) where T : unmanaged
        {
            T temp;
            var p = arr[end];
            var i = start;
 
            for (int j = start; j <= end - 1; j++)
            {
                if (comp(arr[j], p))
                {
                    i++;
                    temp = arr[i];
                    arr[i] = arr[j];
                    arr[j] = temp;
                }
            }
 
            temp = arr[i + 1];
            arr[i + 1] = arr[end];
            arr[end] = temp;
            return i + 1;
        }
    }
}