using System.Collections.Generic;

namespace Joonaxii.Text.Compression
{
    public class TTCChunkComparer : IComparer<(int[], HashSet<int>)>
    {
        public int Compare((int[], HashSet<int>) x, (int[], HashSet<int>) y) => y.Item1.Length.CompareTo(x.Item1.Length);
    }
}