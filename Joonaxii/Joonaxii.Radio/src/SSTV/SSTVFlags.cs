namespace Joonaxii.Radio
{
    [System.Flags]
    public enum SSTVFlags
    {
        None                = 0,
                            
        RequireExactWidth   = 1,
        RequireExactHeight  = 2,
                            
        CenterX             = 4,
        CenterY             = 8,

        SendNewOnOverflow   = 16,

        Default = RequireExactWidth | RequireExactHeight,
    }
}