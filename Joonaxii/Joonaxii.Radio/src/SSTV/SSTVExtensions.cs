using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Joonaxii.Radio
{
    public static class SSTVExtensions
    {
        public static bool GetResolution(this SSTVProtocol protocol, out int width, out int height)
        {
            switch (protocol)
            {
                default:
                    width = 0;
                    height = 0;
                    return false;

                case SSTVProtocol.RBW8_R:
                case SSTVProtocol.RBW8_G:
                case SSTVProtocol.RBW8_B:
                    width = 120;
                    height = 160;
                    return true;
                case SSTVProtocol.Robot12:
                    width = 160;
                    height = 120;
                    return true;

                case SSTVProtocol.Robot24:
                    width = 320;
                    height = 120;
                    return true;

                case SSTVProtocol.Robot36:
                case SSTVProtocol.Robot72:

                case SSTVProtocol.MartinHQ1:
                case SSTVProtocol.MartinHQ2:
                    width = 320;
                    height = 240;
                    return true;

                case SSTVProtocol.RBW12_R:
                case SSTVProtocol.RBW12_G:
                case SSTVProtocol.RBW12_B:
                case SSTVProtocol.RBW24_R:
                case SSTVProtocol.RBW24_G:
                case SSTVProtocol.RBW24_B:
                case SSTVProtocol.RBW36_R:
                case SSTVProtocol.RBW36_G:
                case SSTVProtocol.RBW36_B:
                    width = 240;
                    height = 320;
                    return true;

                case SSTVProtocol.Martin3:
                case SSTVProtocol.Martin4:
                    width = 320;
                    height = 128;
                    return true;

                case SSTVProtocol.Scottie3:
                case SSTVProtocol.Scottie4:
                    width = 320;
                    height = 128;
                    return true;

                case SSTVProtocol.Martin1:
                case SSTVProtocol.Martin2:

                case SSTVProtocol.Scottie1:
                case SSTVProtocol.Scottie2:
                case SSTVProtocol.ScottieDX:
                case SSTVProtocol.ScottieDX2:

                case SSTVProtocol.PD50:
                case SSTVProtocol.PD90:
                    width = 320;
                    height = 256;
                    return true;

                case SSTVProtocol.Pasokon3:
                case SSTVProtocol.Pasokon5:
                case SSTVProtocol.Pasokon7:
                    width = 640;
                    height = 480;
                    return true;

                case SSTVProtocol.PD120:
                case SSTVProtocol.PD180:
                case SSTVProtocol.PD240:
                    width = 640;
                    height = 496;
                    return true;

                case SSTVProtocol.PD160:
                    width = 512;
                    height = 400;
                    return true;

                case SSTVProtocol.PD290:
                    width = 800;
                    height = 616;
                    return true;
            }
        }
    }
}
