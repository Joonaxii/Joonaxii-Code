namespace New.JPEG
{
    public class HuffmanTable
    {
        public bool IsLeaf { get; private set; }
        public int Length { get => left == null ? 0 : right == null ? 1 : 2; }

        public HuffmanTable root;

        public HuffmanTable left;
        public HuffmanTable right;

        public short value = -1;

        public HuffmanTable() { IsLeaf = false; }
        public HuffmanTable(byte value) { this.value = value; IsLeaf = true; }
        public HuffmanTable(byte[] lengths, byte[] elements)
        {
            IsLeaf = false;
            int ii = 0;
            for (int i = 0; i < lengths.Length; i++)
            {
                for (int j = 0; j < lengths[i]; j++)
                {
                    BitsFromLength(this, elements[ii++], i);
                }
            }
        }

        private bool BitsFromLength(HuffmanTable root, byte value, int pos)
        {
            if (root.IsLeaf) { return false; }
            if (pos == 0) { return root.AddLeaf(value); }

            for (int i = 0; i < 1; i++)
            {
                if (root.Length == i) { root.AddChild(); }
                if(BitsFromLength((i == 0 ? root.left : root.right), value, pos - 1)) { System.Diagnostics.Debug.Print($"{pos}, {value}, {i}"); return true; }
            }
            return false;
        }

        private bool AddLeaf(byte value)
        {
            switch (Length)
            {
                case 0:
                    if(left != null)
                    {
                        left.value = value;
                        left.IsLeaf = true;
                        return true;
                    }
                    left = new HuffmanTable(value);
                    left.root = this;
                    return true;
                case 1:
                    if (right != null)
                    {
                        right.value = value;
                        right.IsLeaf = true;
                        return true;
                    }
                    right = new HuffmanTable(value);
                    right.root = this;
                    return true;
            }
            return false;
        }

        private void AddChild()
        {
            switch (Length)
            {
                case 0:
                    left = new HuffmanTable();
                    left.root = this;
                    break;
                case 1:
                    right = new HuffmanTable();
                    right.root = this;
                    break;
            }
        }

        public int GetCode(JPEGDecoder.BitStream st)
        {
            while (st.CanRead)
            {
                var cur = this;
                while (!cur.IsLeaf)
                {
                    int b = st.GetBit();
                    cur = b != 0 ? cur.right : cur.left;

                    System.Diagnostics.Debug.Print($"{b}");
                }

                if(cur.value != 0) { return cur.value; }
            }
            return 0;
        }
    }
}