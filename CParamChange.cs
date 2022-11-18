using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BuzzGUI.Common;
using Buzz.MachineInterface;
using BuzzGUI.Interfaces;

namespace Snapshot
{
    internal class CParamChange
    {
        private readonly IParameter Parameter;
        private readonly int track;
        private readonly int targetValue;

        public bool Finished { get; private set; }

        public CParamChange(IParameter param, int track, int value)
        {
            Finished = false;
            Parameter = param;
            this.track = track;
            targetValue = value;
        }

        public void Work()
        {
            Parameter.SetValue(track, targetValue);
            Finished = true;
        }
    }
}
