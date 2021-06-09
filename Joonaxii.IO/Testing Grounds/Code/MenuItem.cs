namespace Testing_Grounds
{    
    public abstract class MenuItem
    {
        public string name;
        public bool enabled;

        protected MenuItem(string name, bool enabled = true)
        {
            this.name = name;
            this.enabled = enabled;
        }

        public abstract bool OnClick();
    }
}