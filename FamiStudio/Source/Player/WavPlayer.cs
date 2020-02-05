using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FamiStudio
{
    class WavPlayer : BasePlayer
    {
        List<short> outputBuffer;

        public WavPlayer(List<short> buffer) : base(NesApu.APU_WAV_EXPORT)
        {
            outputBuffer = buffer;
        }

        protected override short[] EndFrame()
        {
            var samples = base.EndFrame();
            outputBuffer.AddRange(samples);
            return samples;
        }
    }
}
