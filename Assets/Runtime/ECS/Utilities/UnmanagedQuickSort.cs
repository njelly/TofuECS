namespace Tofunaut.TofuECS.Utilities
{
    public static unsafe class UnmanagedQuickSort
    {
        public delegate bool Comparison<T>(T a, T b) where T : unmanaged;

        public static void Sort<T>(T* arr, int length, Comparison<T> comp) where T : unmanaged =>
            SortInternal(arr, 0, length - 1, comp);

        private static void SortInternal<T>(T* arr, int left, int right, Comparison<T> comp) where T : unmanaged 
        {
            if(left >= right)
                return;

            var pivot = Partition(arr, left, right, comp);
            SortInternal(arr, left, pivot - 1, comp);
            SortInternal(arr, pivot + 1, right, comp);
        }

        private static void Swap<T>(T* arr, int a, int b) where T : unmanaged
        {
            (arr[a], arr[b]) = (arr[b], arr[a]);
        }  

        private static int Partition<T>(T* arr, int left, int right, Comparison<T> comp) where T : unmanaged
        {
            var q = right;
            while ( q > left ) {
                while (comp(arr[left], arr[right]))
                    left++;
                while (!comp(arr[right], arr[left]) )
                    right--;
                if (comp(arr[right], arr[left])) {
                    Swap(arr, left,q);
                }
            }
            Swap(arr, left, q);
            return q ;
        }
    }
}