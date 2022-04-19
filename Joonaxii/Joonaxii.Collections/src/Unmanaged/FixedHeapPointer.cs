using System.Runtime.InteropServices;

namespace Joonaxii.Collections.Unmanaged
{
    [StructLayout(LayoutKind.Sequential, Size=8)]
    public struct FixedHeapPointer
    {
        public int start;
        public int length;

        public FixedHeapPointer(int start, int length)
        {
            this.start = start;
            this.length = length;
        }
    }
}