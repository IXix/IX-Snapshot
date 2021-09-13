using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows.Forms;

using BuzzGUI.Common;
using Buzz.MachineInterface;
using BuzzGUI.Interfaces;

namespace Snapshot
{
    public class MachineGUIFactory : IMachineGUIFactory
    {
        public IMachineGUI CreateGUI(IMachineGUIHost host) { return new GUI(); }
    }

    [MachineDecl(Name = "IX Snapshot", ShortName = "Snapshot", Author = "IX", MaxTracks = 0, InputCount = 0, OutputCount = 0)]
    public class Machine : IBuzzMachine, INotifyPropertyChanged
    {
        IBuzzMachineHost host;

    #region IBuzzMachine

        public Machine(IBuzzMachineHost host)
        {
            this.host = host;
            States = new ObservableCollection<MachineState>();

            var machines = Global.Buzz.Song.Machines;
            foreach (var m in machines)
            {
                States.Add(new MachineState(m));
            }

            Global.Buzz.Song.MachineAdded += (m) => { OnMachineAdded(m); };
            Global.Buzz.Song.MachineRemoved += (m) => { OnMachineRemoved(m); };
        }

        // Class to hold whatever we eventually decide needs saving to the song
        // It will be dumped as a byte array
        public class MachineStateData
        {
            public UInt16 Version = 1;
            // FIXME: Find an appropriate storage class to dump data into.
            // Can't rely on the structure staying the same so thinking version number
            // is the only named property and everything else gets stored as raw bytes.
            // Look at binaryWriter
        }

        // This is how Save/Load/Init get handled
        // MachineState can be any class at all, 'MachineStateData' isn't part of the spec.
        // get calls CMachineInterfaceEx::Load() and CMachineInterface::Init() if there's data to restore
        // set calls CMachineInterface::Save() 
        public MachineStateData MachineState
        {
            get
            {
                return new MachineStateData();
            }
            set
            {
                switch(value.Version)
                {
                    case 1:                        
                        break; // FIXME: Load stuff

                    default:                       
                        break; // FIXME: Error
                }
            }
        }

        public void Stop()
        {
            // FIXME - If restore on stop
        }

        // Called after song load or template drop
        public void ImportFinished(IDictionary<string, string> machineNameMap)
        {
            // FIXME - Remap stored states to correct machines using machineNameMap
        }

    #endregion IBuzzMachine

        private void OnMachineAdded(IMachine m)
        {
            States.Add(new MachineState(m));
        }

        private void OnMachineRemoved(IMachine m)
        {
            States.RemoveAt(States.FindIndex(x => x.Machine == m));
        }

        public ObservableCollection<MachineState> States { get; set; }

        // Global params
        int m_slot;
        [ParameterDecl(IsStateless = false, MinValue = 1, MaxValue = 128, DefValue = 1, Description = "Active slot", Name = "Slot")]
        public int Slot
        {
            get { return m_slot; }
            set
            {
                m_slot = value;
                // FIXME: Do something useful
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        internal void Capture()
        {
            foreach (var s in States)
            {
                if (s.Selected)
                    s.Capture();
            }
        }

        internal void CaptureMissing()
        {
            foreach (var s in States)
            {
                if (s.Selected && !s.GotState)
                    s.Capture();
            }
        }

        internal void Restore()
        {
            foreach (var s in States)
            {
                if (s.GotState)
                    s.Restore();
            }
        }

        internal void Purge()
        {
            foreach (var s in States)
            {
                if (s.GotState && !s.Selected)
                    s.Clear();
            }
        }

        internal void Clear()
        {
            foreach (var s in States)
            {
                if (s.GotState)
                    s.Clear();
            }
        }
    }
}
