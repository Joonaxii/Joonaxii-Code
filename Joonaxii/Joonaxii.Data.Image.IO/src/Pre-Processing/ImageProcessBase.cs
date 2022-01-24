using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Joonaxii.Data.Image.Conversion.Processing
{
    public abstract class ImageProcessBase<T>
    {
        public abstract bool ModifiesImage { get; }
        public abstract T Process(IPixelProvider pixels, int width, int height, byte bpp);
    }
}
