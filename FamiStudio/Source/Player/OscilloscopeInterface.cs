using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FamiStudio
{
    public interface IOscilloscope
    {
        void AddSamples(short[] samples, int trigger = NesApu.TRIGGER_NONE);
    }
}
