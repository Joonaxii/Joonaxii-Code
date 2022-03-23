namespace Joonaxii.Collections
{
    [System.Flags]
    public enum HeapTrimFlags
    {
        None = 0,

        /// <summary>
        /// Retain values that are already in the heap
        /// </summary>
        Retain = 1,

        /// <summary>
        /// Force the heap's capacity to be next power of 2
        /// </summary>
        ForcePowOf2 = 2,

        /// <summary>
        /// If ForcePowOf2 is set, forces each region's capacity to be pow of 2
        /// </summary>
        ForceP2Regions = 4,
    }
}