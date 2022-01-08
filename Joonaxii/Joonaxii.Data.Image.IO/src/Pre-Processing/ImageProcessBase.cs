using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Joonaxii.Data.Image.IO.Processing
{
    public abstract class ImageProcessBase<T>
    {
        public abstract bool ModifiesImage { get; }
        public abstract T Process(FastColor[] pixels, int width, int height, byte bpp);
    }
}
