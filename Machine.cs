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
    public class Machine : IBuzzMachine
    {
        IBuzzMachineHost host;

        public ObservableCollection<MachineState> States { get; set; }

        public SnapshotVM VM { get; }

        private List<IPropertyState> _allProperties;

        public bool GotState { get; private set; }
        public bool SelectNewMachines { get; set; }
        public bool CaptureOnSlotChange { get; set; }
        public bool RestoreOnSlotChange { get; set; }
        public bool RestoreOnSongLoad { get; set; }
        public bool RestoreOnStop { get; set; }

        // How many states are selected
        public int SelCount
        {
            get { return _allProperties.Count(x => x.Selected == true); }
        }

        // How many states have been captured
        public int StoredCount
        {
            get { return _allProperties.Count(x => x.GotValue == true); }
        }

        // How many states are stored that aren't selected
        public int RedundantCount
        {
            get { return _allProperties.Count(x => x.Selected == false && x.GotValue == true); }
        }

        // How many selected states have not been captured
        public int MissingCount
        {
            get { return _allProperties.Count(x => x.Selected == true && x.GotValue == false); }
        }

        #region IBuzzMachine

        public Machine(IBuzzMachineHost host)
        {
            this.host = host;
            States = new ObservableCollection<MachineState>();
            VM = new SnapshotVM(this);
            _allProperties = new List<IPropertyState>();

            ReadOnlyCollection<IMachine> machines = Global.Buzz.Song.Machines;
            foreach (IMachine m in machines)
            {
                OnMachineAdded(m);
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
            if(RestoreOnStop)
            {
                Restore();
            }
        }

        // Called after song load or template drop
        public void ImportFinished(IDictionary<string, string> machineNameMap)
        {
            // FIXME - Remap stored states to correct machines using machineNameMap
            //if(RestoreOnSongLoad)
            //{
            //    Restore();
            //}
        }

        #endregion IBuzzMachine

        #region events
        private void OnMachineAdded(IMachine m)
        {
            if (m.ManagedMachine != this)
            {
                MachineState ms = new MachineState(m);
                foreach (IPropertyState s in ms.AllProperties)
                {
                    s.SelChanged += OnStateChanged;
                    s.ValChanged += OnStateChanged;
                    s.Selected = SelectNewMachines;
                    _allProperties.Add(s);
                }
                States.Add(ms);
                VM.AddState(ms);
            }
        }

        private void OnStateChanged(object sender, StateChangedEventArgs e)
        {
            VM.SelectionInfo = string.Format("{0} of {1} properties selected\n{2} stored\n{3} missing\n{4} redundant", SelCount, _allProperties.Count, StoredCount, MissingCount, RedundantCount);
        }

        private void OnMachineRemoved(IMachine m)
        {
            if (m != host.Machine)
            {
                MachineState s = States[States.FindIndex(x => x.Machine == m)];
                States.Remove(s);
                VM.RemoveState(s);
            }
        }
        #endregion events

        #region Global Parameters
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

        #endregion Global Parameters

        #region Commands

        internal void Capture()
        {
            foreach (MachineState s in States)
            {
                if (s.Capture()) GotState = true;
            }
        }

        internal void CaptureMissing()
        {
            foreach (MachineState s in States)
            {
                if(s.CaptureMissing()) GotState = true;
            }
        }

        internal void Restore()
        {
            if (GotState)
            {
                foreach (MachineState s in States)
                {
                    s.Restore();
                }
            }
        }

        internal void Purge()
        {
            if (GotState)
            {
                foreach (var s in States)
                {
                    s.Purge();
                    if (s.GotState) GotState = true;
                }
            }
        }

        internal void Clear()
        {
            if (GotState)
            {
                foreach (MachineState s in States)
                {
                    s.Clear();
                }
                GotState = false;
            }
        }

        #endregion Commands
    }
}
