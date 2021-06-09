namespace Testing_Grounds
{
    public class QuitItem : MenuItem
    {
        public QuitItem() : base("Quit", true)
        {
        }

        public override bool OnClick()
        {
            return false;
        }
    }
}