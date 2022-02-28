using System.Collections.Generic;

namespace New.JPEG
{
    public class HuffmanTable
    {
        public bool IsLeaf { get => value > -1; }
        public int Length { get => left == null ? 0 : right == null ? 1 : 2; }
        public bool IsEmpty { get => (left == null | right == null) & !IsLeaf; }

        public bool IsRight { get => root != null ? root.right == this : false; }

        public HuffmanTable root;

        public HuffmanTable left;
        public HuffmanTable right;

        public short value = -1;

        public HuffmanTable(HuffmanTable root) { this.root = root; }

        public void AssignLeaf(short value)
        {
            this.value = value;
        }

        public HuffmanTable FindNext(int depth)
        {
            if(depth < 0)
            {
                if(left == null)
                {
                    return left = new HuffmanTable(this);
                }
                if (left.IsEmpty) { return left; }

                if (right == null)
                {
                    return right = new HuffmanTable(this);
                }
                if (right.IsEmpty) { return right; }
                return null;
            }

            var l = left?.FindNext(depth - 1);
            if(l != null) { return l; }
            return right?.FindNext(depth - 1);
        }

        public HuffmanTable FindNextNonLeaf(int depth)
        {
            if (depth < 0)
            {
                if (left == null)
                {
                    return left = new HuffmanTable(this);
                }
                if (!left.IsLeaf) { return left; }

                if (right == null)
                {
                    return right = new HuffmanTable(this);
                }
                if (!right.IsLeaf) { return right; }
                return null;
            }

            var l = left?.FindNext(depth - 1);
            if (l != null) { return l; }
            return right?.FindNext(depth - 1);
        }

        public HuffmanTable FindNextLeaf(int depth)
        {
            if (depth <= 0)
            {
                return left.IsEmpty ? left : right.IsEmpty ? right : null;
            }

            var l = left?.FindNext(depth - 1);
            if (l != null) { return l; }
            return right?.FindNext(depth - 1);
        }

        public void FindAllNodes(List<HuffmanTable> nodes)
        {
            if (IsLeaf) { nodes.Add(this); return; }

            left?.FindAllNodes(nodes);
            right?.FindAllNodes(nodes);
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