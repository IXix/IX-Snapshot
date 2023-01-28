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
        internal readonly IParameter Parameter;
        internal readonly int track;
        private readonly int targetValue;
        private readonly int initialValue;
        private readonly int shape;
        private double phase = 0;

        private double duration = 0;
        private double elapsedTime = 0;

        public bool Finished { get; private set; }

        public CParamChange(IParameter param, int track, int value, double duration = 0, int shape = 0)
        {
            Finished = false;
            Parameter = param;
            this.track = track;
            this.duration = duration;
            this.shape = shape;
            initialValue = Parameter.GetValue(track);
            targetValue = value;
        }

        public void Work(int numsamples)
        {
            if (duration < 1)
            {
                Parameter.SetValue(track, targetValue);
                Finished = true;
                return;
            }

            elapsedTime += numsamples;

            double t = elapsedTime / duration;
            phase = t; // linear by default

            switch (shape)
            // shapes from https://www.desmos.com/calculator/k45ggnkiot
            {
                //case 0: // Reaper 1 == linear

                case 1:
                    phase = t * (2 - t); // Reaper 2
                    break;

                case 2:
                    phase = t * t; // Reaper 3
                    break;

                case 3:
                    phase = 1 - Math.Pow(1 - t, 4); // Reaper 4
                    break;

                case 4:
                    phase = Math.Pow(t, 4); // Reaper 5
                    break;

                case 5:
                    phase = (t * t) - (3 - t * 2); // Reaper 6
                    break;

                case 6: // Reaper 7
                    if(phase <= 0.5)
                    {
                        phase = 8 * Math.Pow(t, 4);
                    }
                    else
                    {
                        phase = 1 - 8 * Math.Pow(1 - t, 4);
                    }
                    break;

                default:
                    break;
            }

            int currentValue = (int)Math.Round(initialValue + (targetValue - initialValue) * phase);

            Parameter.SetValue(track, currentValue);

            if (elapsedTime >= duration)
                Finished = true;
        }



    }
}
