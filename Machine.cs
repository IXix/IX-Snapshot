using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;

using BuzzGUI.Common;
using Buzz.MachineInterface;
using BuzzGUI.Interfaces;
using System.IO;

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

        private IMachine ThisMachine { get; set; }
        private IParameter SlotParam { get; set; }

        public ObservableCollection<MachineState> States { get; private set; }

        public SnapshotVM VM { get; }

        private List<IPropertyState> _allProperties;

        public bool GotState => _allProperties.Count(x => x.GotValue) > 0;

        private List<string> m_slotNames;
        public List<string> SlotNames
        {
            get => m_slotNames;
            private set
            {
                m_slotNames = value;
                if(!loading)
                {
                    OnPropertyChanged("SlotNames");
                    OnPropertyChanged("SlotName");
                }
            }
        }
        public string SlotName
        {
            get => SlotNames.ElementAt(Slot);
            set
            {
                if(value != null)
                    SlotNames[Slot] = value;
            }
        }

        private bool m_selectNewMachines;
        public bool SelectNewMachines
        {
            get => m_selectNewMachines;
            set
            {
                if(m_selectNewMachines != value)
                {
                    m_selectNewMachines = value;
                    OnPropertyChanged("SelectNewMachines");
                }
            }
        }

        private bool m_captureOnSlotChange;
        public bool CaptureOnSlotChange
        {
            get => m_captureOnSlotChange;
            set
            {
                if (m_captureOnSlotChange != value)
                {
                    m_captureOnSlotChange = value;
                    OnPropertyChanged("CaptureOnSlotChange");
                }
            }
        }

        private bool m_restoreOnSlotChange;
        public bool RestoreOnSlotChange
        {
            get => m_restoreOnSlotChange;
            set
            {
                if (m_restoreOnSlotChange != value)
                {
                    m_restoreOnSlotChange = value;
                    OnPropertyChanged("RestoreOnSlotChange");
                }
            }
        }

        private bool m_restoreOnSongLoad;
        public bool RestoreOnSongLoad
        {
            get => m_restoreOnSongLoad;
            set
            {
                if (m_restoreOnSongLoad != value)
                {
                    m_restoreOnSongLoad = value;
                    OnPropertyChanged("RestoreOnSongLoad");
                }
            }
        }

        private bool m_restoreOnStop;
        public bool RestoreOnStop
        {
            get => m_restoreOnStop;
            set
            {
                if (m_restoreOnStop != value)
                {
                    m_restoreOnStop = value;
                    OnPropertyChanged("RestoreOnStop");
                }
            }
        }

        // How many properties are selected
        public int SelCount
        {
            get { return _allProperties.Count(x => x.Selected == true); }
        }

        // How many properties have been captured
        public int StoredCount
        {
            get { return _allProperties.Count(x => x.GotValue == true); }
        }

        // How many properties are stored that aren't selected
        public int RedundantCount
        {
            get { return _allProperties.Count(x => x.Selected == false && x.GotValue == true); }
        }

        // How many selected properties have not been captured
        public int MissingCount
        {
            get { return _allProperties.Count(x => x.Selected == true && x.GotValue == false); }
        }

        // Size of data in current slot
        public int Size
        {
            get
            {
                int size = 0;
                foreach(IPropertyState s in _allProperties)
                {
                    size += s.Size;
                }
                return size;
            }
        }

        // Size of data in all slots
        public int TotalSize
        {
            get
            {
                int size = 0;
                foreach (IPropertyState s in _allProperties)
                {
                    size += s.TotalSize;
                }
                return size;
            }
        }

        #region IBuzzMachine

        public Machine(IBuzzMachineHost host)
        {
            this.host = host;
            m_loadedState = new MachineStateData();

            SlotNames = new List<string>();
            for(int i = 0; i < 128; i++)
            {
                SlotNames.Add(string.Format("Slot {0}", i));
            }

            States = new ObservableCollection<MachineState>();
            _allProperties = new List<IPropertyState>();
            VM = new SnapshotVM(this);

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
            public MachineStateData()
            {
                data = new byte[0];
            }

            public MachineStateData(Machine m)
            {
                MemoryStream stream = new MemoryStream();
                BinaryWriter w = new BinaryWriter(stream);

                // Options
                w.Write(m.SelectNewMachines);
                w.Write(m.CaptureOnSlotChange);
                w.Write(m.RestoreOnSlotChange);
                w.Write(m.RestoreOnSongLoad);
                w.Write(m.RestoreOnStop);

                // Slot names
                foreach (string s in m.SlotNames)
                {
                    w.Write(s);
                }

                // State data
                foreach (MachineState s in m.States)
                {
                    w.Write(s.Machine.Name);
                    var saveProperties = s.AllProperties.Where(x => x.Selected || x.NonEmpty);
                    w.Write((Int32)saveProperties.Count());
                    foreach(PropertyBase p in saveProperties)
                    {
                        p.WriteData(w);
                    }
                }

                data = stream.ToArray();
            }

            public Byte version = 1;
            public byte [] data;
        }

        // This is how Save/Load/Init get handled
        // MachineState can be any class at all, 'MachineStateData' isn't part of the spec.
        // get calls CMachineInterfaceEx::Load() and CMachineInterface::Init() if there's data to restore
        // set calls CMachineInterface::Save() 
        private MachineStateData m_loadedState;
        private bool loading = false;
        public MachineStateData MachineState
        {
            get
            {
                return new MachineStateData(this);
            }
            set
            {
                try
                {
                    loading = true;
                    m_loadedState = value;
                }
                catch(Exception e)
                {
                    m_loadedState = new MachineStateData(this);
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

        private void RestoreLoadedData()
        {
            if (m_loadedState.data.Length == 0) return;

            if (m_loadedState.version > 1) throw new Exception("Version mismatch");

            MemoryStream stream = new MemoryStream(m_loadedState.data);
            BinaryReader r = new BinaryReader(stream);

            // Options
            SelectNewMachines = r.ReadBoolean();
            CaptureOnSlotChange = r.ReadBoolean();
            RestoreOnSlotChange = r.ReadBoolean();
            RestoreOnSongLoad = r.ReadBoolean();
            RestoreOnStop = r.ReadBoolean();

            // Slot names
            for (int i = 0; i < 128; i++)
            {
                SlotNames[i] = r.ReadString();
            }

            // State data
            while(r.PeekChar() > -1)
            {
                string name = r.ReadString(); // Machine name
                try
                {
                    MachineState s = States.Single(x => x.Machine.Name == name);

                    Int32 count = r.ReadInt32(); // Number of saved properties
                    for(Int32 i = 0; i < count; i++)
                    {
                        name = r.ReadString();
                        try
                        {
                            IPropertyState ps = s.AllProperties.Single(x => x.Name == name);
                            ps.ReadData(r, m_loadedState.version);
                        }
                        catch (Exception e)
                        {
                            continue;
                        }
                    }
                }
                catch (Exception e)
                {
                    return;
                }
            }
        }

        // Called after song load or template drop
        public void ImportFinished(IDictionary<string, string> machineNameMap)
        {
            RestoreLoadedData();

            loading = false;

            if(RestoreOnSongLoad)
            {
                Restore();
            }
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
            else
            {
                ThisMachine = host.Machine;
                SlotParam = ThisMachine.ParameterGroups[1].Parameters[0];
            }
        }

        private void OnStateChanged(object sender, StateChangedEventArgs e)
        {
            if(VM != null)
                VM.SelectionInfo = string.Format("{0} of {1} properties selected\n{2} stored\n{3} missing\n{4} redundant\nSlot size: {5}\nTotal Size: {6}", SelCount, _allProperties.Count, StoredCount, MissingCount, RedundantCount, Misc.ToSize(Size), Misc.ToSize(TotalSize));
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
        [ParameterDecl(IsStateless = false, MinValue = 0, MaxValue = 127, DefValue = 1, Description = "Active slot", Name = "Slot")]
        public int Slot
        {
            get => m_slot;
            set
            {
                if (m_slot != value)
                {
                    OnSlotChanging();

                    m_slot = value;
                    foreach (MachineState s in States)
                    {
                        s.Slot = m_slot;
                    }

                    // This is to update the parameter if the slot change comes from the combo
                    if (SlotParam != null && SlotParam.GetValue(0) != m_slot)
                    {
                        Application.Current.Dispatcher.BeginInvoke((Action)(() => { SlotParam.SetValue(0, m_slot); }), DispatcherPriority.Send);
                    }

                    OnSlotChanged();
                }
            }
        }

        #endregion Global Parameters

        #region Commands

        internal void Capture()
        {
            foreach (MachineState s in States)
            {
                s.Capture();
            }
        }

        internal void CaptureMissing()
        {
            foreach (MachineState s in States)
            {
                s.CaptureMissing();
            }
        }

        internal void Restore()
        {
            if (GotState)
            {
                /*
                foreach (MachineState s in States)
                {
                    s.Restore();
                }
                */

                MachineSnapshot sn = new MachineSnapshot();
                foreach (MachineState s in States)
                {
                    sn.AddState(s);
                }

                Application.Current.Dispatcher.BeginInvoke((Action)(() => { sn.Apply(); }), DispatcherPriority.Send);
            }
        }

        internal void Purge()
        {
            if (GotState)
            {
                foreach (var s in States)
                {
                    s.Purge();
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
            }
        }


        // Called before the slot is changed
        internal void OnSlotChanging()
        {
            if(CaptureOnSlotChange && !loading)
            {
                Capture();
            }
        }

        // Called after the slot has changed
        public EventHandler SlotChanged;


        internal void OnSlotChanged()
        {
            if (RestoreOnSlotChange && !loading)
            {
                Restore();
            }

            EventHandler handler = SlotChanged;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        #endregion Commands

        public event PropertyChangedEventHandler PropertyChanged;
        public virtual void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
