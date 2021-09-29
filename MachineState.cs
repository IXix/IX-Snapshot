using BuzzGUI.Interfaces;
using BuzzGUI.Common.Presets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Snapshot
{
    public class MachineState
    {
        public MachineState(IMachine m)
        {
            Machine = m;
            UseData = true;
            m_gotState = false;
            m_preset = null;
        }

        public IMachine Machine { get; set; }

        private bool m_gotState;
        public bool GotState { get { return m_gotState; } }

        // Whether to include save data in the preset
        public bool UseData { get; set; }

        // State storage
        private Preset m_preset;

        public bool Capture()
        {
            // Capture everything
            m_preset = new Preset(Machine, false, true);
            m_gotState = true;
            return m_gotState;
        }

        public bool Restore()
        {
            // FIXME:
            // We want to control what gets restored so Preset.Apply() won't work
            //m_preset.Apply(Machine, UseData);
            return true;
        }

        public void Clear()
        {
            m_gotState = false;
            m_preset = null;
        }
    }
}
