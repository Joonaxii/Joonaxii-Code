using Joonaxii.IO;
using Joonaxii.MathJX;
using System.Collections.Generic;
using System.Text;

namespace Joonaxii.Image.Codecs.JPEG
{
    internal class JPEGHuffmanNode
    {
        public JPEGHuffmanNode root;
        public bool IsLeaf { get => (left == null & right == null) & value > -1; }

        public int value = -1;

        public JPEGHuffmanNode left;
        public JPEGHuffmanNode right;

        public JPEGHuffmanNode() { }
        public JPEGHuffmanNode(JPEGHuffmanNode root) { this.root = root; }

        public byte Read(BitReader br)
        {
            JPEGHuffmanNode cur = this;
            while (!cur.IsLeaf)
            {
                cur = br.ReadBoolean() ? cur.right : cur.left;
                if(cur == null) { return 0; }
            }
            return (byte)cur.value;
        }

        public void DebugPrint()
        {
            int width = 32 * 2 + 1;
            int height = 24 + 1;
            char[] buffer = new char[width * height];
            BufferUtils.Memset(buffer, ' ');
            DrawNode(width >> 1, 0, width, buffer, 2);

            char[] buf = new char[width];
            for (int i = 0; i < height; i++)
            {
                System.Buffer.BlockCopy(buffer, i * width * 2, buf, 0, width * 2);
                System.Diagnostics.Debug.Print($"{new string(buf)}\n");
            }
        }

        private void DrawNode(int x, int y, int w, char[] buffer, int depth)
        {
            int i = y * w + x;
            if(x >= w | i >= buffer.Length) { return; }
            buffer[i] = IsLeaf ? 'X' : 'o';
            if (IsLeaf) { return; }

            i = y * w + x - 1;
            buffer[i] = '/';
            left.DrawNode(x - depth - 1, y + 1, w, buffer, depth << 1);

            i = y * w + x + 1;
            buffer[i] = '\\';
            right.DrawNode(x + depth + 1, y + 1, w, buffer, depth << 1);

        }

        public static JPEGHuffmanNode GetRightLevelNode(JPEGHuffmanNode node)
        {
            if(node == null) { return null; }

            if(node.root != null && node.root.left == node)
            {
                return node.root.right;
            }

            int count = 0;

            var nd = node;
            while(nd.root != null && nd.root.right == nd)
            {
                nd = nd.root;
                count++;
            }

            if(nd.root == null) { return null; }

            nd = nd.root.right;

            while (count > 0)
            {
                nd = nd.left;
                count--;
            }
            return nd;
        }

        private void Print2D(JPEGHuffmanNode root, StringBuilder sb, int space)
        {
            if(root == null) { return; }

            space += 10;

        }

        public void PrintTree()
        {
            StringBuilder sb = new StringBuilder();
            Stack<JPEGHuffmanNode> stack = new Stack<JPEGHuffmanNode>();
            Stack<bool> bit = new Stack<bool>();
            stack.Push(this);
    
            while (stack.Count > 0)
            {
                JPEGHuffmanNode node = stack.Pop();

                if (node.left != null) { stack.Push(node.left); }
                if (node.right != null) { stack.Push(node.right); }

                if (node.IsLeaf)
                {
                    var n = node;
                    var r = node.root;
                    while(r != null)
                    {
                        bit.Push(r.right == n);
                        n = r;
                        r = r.root;
                    }

                    while(bit.Count > 0)
                    {
                        sb.Append($"{(bit.Pop() ? '1' : '0')}");
                    }
                    sb.AppendLine($":{node.value}:");
                }
            }
            System.Diagnostics.Debug.Print(sb.ToString());
        }
    }
}