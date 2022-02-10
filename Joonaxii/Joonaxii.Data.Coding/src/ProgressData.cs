namespace Joonaxii.Data
{
    public struct ProgressData
    {
        public string title;
        public float mainProgress;
        public int curMain;
        public int maxMain;

        public string subTitle;
        public float subProgress;
        public int curSub;
        public int maxSub;

        public ProgressData(string title, float mainProgress, int curMain, int maxMain) : this()
        {
            this.title = title;
            this.mainProgress = mainProgress;
            this.curMain = curMain;
            this.maxMain = maxMain;
        }

        public ProgressData(string title, float mainProgress, int curMain, int maxMain, string subTitle, float subProgress, int curSub, int maxSub)
        {
            this.title = title;
            this.mainProgress = mainProgress;
            this.curMain = curMain;
            this.maxMain = maxMain;
            this.subTitle = subTitle;
            this.subProgress = subProgress;
            this.curSub = curSub;
            this.maxSub = maxSub;
        }
    }
}