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
        [ParameterDecl(IsStateless = false, MinValue = 1, MaxValue = 128, DefValue = 1, Description = "Active slot", Name = "Slot")]
        public int Slot { get; set; }

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
