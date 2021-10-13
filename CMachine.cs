﻿using System;
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
    public class CMachineGUIFactory : IMachineGUIFactory
    {
        public IMachineGUI CreateGUI(IMachineGUIHost host) { return new GUI(); }
    }

    [MachineDecl(Name = "IX Snapshot", ShortName = "Snapshot", Author = "IX", MaxTracks = 0, InputCount = 0, OutputCount = 0)]
    public class CMachine : IBuzzMachine, INotifyPropertyChanged
    {
        IBuzzMachineHost host;

        private IMachine ThisMachine { get; set; }
        private IParameter SlotParam { get; set; }

        public ObservableCollection<CMachineState> States { get; private set; }

        public CSnapshotVM VM { get; }

        public List<IPropertyState> AllProperties { get; private set; }

        private List<CMachineSnapshot> _slots;
        public List<CMachineSnapshot> Slots => _slots;

        public CMachineSnapshot CurrentSlot => _slots[_slot];

        public string SlotName
        {
            get => CurrentSlot.Name;
            set
            {
                if(value != null && value != CurrentSlot.Name)
                {
                    CurrentSlot.Name = value;
                    OnPropertyChanged("SlotName");
                    OnPropertyChanged("SlotDetails");
                }
            }
        }

        public string SelectionInfo => string.Format("{0} of {1} properties selected\n{2} stored\n{3} missing\n{4} redundant\nSlot size: {5}\nTotal Size: {6}", SelCount, AllProperties.Count, StoredCount, MissingCount, RedundantCount, Misc.ToSize(Size), Misc.ToSize(TotalSize));

        private bool _selectNewMachines;
        public bool SelectNewMachines
        {
            get => _selectNewMachines;
            set
            {
                if(_selectNewMachines != value)
                {
                    _selectNewMachines = value;
                    OnPropertyChanged("SelectNewMachines");
                }
            }
        }

        private bool _captureOnSlotChange;
        public bool CaptureOnSlotChange
        {
            get => _captureOnSlotChange;
            set
            {
                if (_captureOnSlotChange != value)
                {
                    _captureOnSlotChange = value;
                    OnPropertyChanged("CaptureOnSlotChange");
                }
            }
        }

        private bool _restoreOnSlotChange;
        public bool RestoreOnSlotChange
        {
            get => _restoreOnSlotChange;
            set
            {
                if (_restoreOnSlotChange != value)
                {
                    _restoreOnSlotChange = value;
                    OnPropertyChanged("RestoreOnSlotChange");
                }
            }
        }

        private bool _restoreOnSongLoad;
        public bool RestoreOnSongLoad
        {
            get => _restoreOnSongLoad;
            set
            {
                if (_restoreOnSongLoad != value)
                {
                    _restoreOnSongLoad = value;
                    OnPropertyChanged("RestoreOnSongLoad");
                }
            }
        }

        private bool _restoreOnStop;
        public bool RestoreOnStop
        {
            get => _restoreOnStop;
            set
            {
                if (_restoreOnStop != value)
                {
                    _restoreOnStop = value;
                    OnPropertyChanged("RestoreOnStop");
                }
            }
        }

        // How many properties are selected
        public int SelCount => AllProperties.Count(x => x.Selected);

        // How many properties have been captured
        public int StoredCount => CurrentSlot.StoredCount;

        // How many properties are stored that aren't selected
        public int RedundantCount => CurrentSlot.RedundantCount;

        // How many selected properties have not been captured
        public int MissingCount => AllProperties.Where(x => x.Selected).Except(CurrentSlot.StoredProperties).Count();

        // Size of data in current slot
        public int Size => CurrentSlot.Size;

        // Size of data in all slots
        public int TotalSize
        {
            get
            {
                int size = 0;
                foreach (CMachineSnapshot s in _slots)
                {
                    size += s.Size;
                }
                return size;
            }
        }

        public bool SlotHasData(int index) => CurrentSlot.StoredCount > 0;

        #region IBuzzMachine

        public CMachine(IBuzzMachineHost host)
        {
            this.host = host;
            _loadedState = new CMachineStateData();

            _slots = new List<CMachineSnapshot>();
            for(int i = 0; i < 128; i++)
            {
                _slots.Add(new CMachineSnapshot(this, i));
            }

            States = new ObservableCollection<CMachineState>();
            AllProperties = new List<IPropertyState>();
            VM = new CSnapshotVM(this);

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
        public class CMachineStateData
        {
            public CMachineStateData()
            {
                data = new byte[0];
            }

            /* FILE STRUCTURE
             * 
             * SelectNewMachines
             * CaptureOnSlotChange
             * RestoreOnSlotChange
             * RestoreOnSongLoad
             * RestoreOnStop
             * Slot data (CMachineSnapshot.WriteData() x 128)
             * number of saved states
             * * State name
             * * Machine DLL name
             * * number of properties saved
             * * * Property name
             * * * Property track (-1 if null)
             * * * Property selected
             * * * number of saved snapshot values
             * * * * snapshot index
             * * * * Snapshot data for property (CMachineSnapshot.WriteProperty())
             */
            public CMachineStateData(CMachine m)
            {
                MemoryStream stream = new MemoryStream();
                BinaryWriter w = new BinaryWriter(stream);

                // Options
                w.Write(m.SelectNewMachines);
                w.Write(m.CaptureOnSlotChange);
                w.Write(m.RestoreOnSlotChange);
                w.Write(m.RestoreOnSongLoad);
                w.Write(m.RestoreOnStop);

                // Save slot data
                for (int i = 0; i < 128; i++)
                {
                    m._slots[i].WriteData(w);
                }

                // Build a list of states to save by finding which are referred to by snapshots
                List<CMachineState> saveStates = new List<CMachineState>();
                foreach (CMachineState s in m.States)
                {
                    if(m._slots.Exists(x => x.ContainsMachine(s)))
                    {
                        saveStates.Add(s);
                    }
                }
                w.Write((Int32)saveStates.Count()); 
                foreach (CMachineState s in saveStates)
                {
                    w.Write(s.Machine.Name);
                    w.Write(s.Machine.DLL.Name);

                    // Build a dictionary of properties to save and the snapshots they're referenced by
                    Dictionary<CPropertyBase, List<CMachineSnapshot>> saveProperties = new Dictionary<CPropertyBase, List<CMachineSnapshot>>();
                    foreach(CPropertyBase ps in s.AllProperties)
                    {
                        List<CMachineSnapshot> slots = m._slots.Where(x => x.ContainsProperty(ps)).ToList();
                        if(slots.Count() > 0 || ps.Selected)
                        {
                            saveProperties[ps] = slots;
                        }
                    }
                    w.Write((Int32)saveProperties.Count());
                    foreach(KeyValuePair<CPropertyBase, List<CMachineSnapshot>> item in saveProperties)
                    {
                        CPropertyBase p = item.Key;
                        List<CMachineSnapshot> slots = item.Value;
                        w.Write(p.Name);
                        w.Write(p.Track ?? -1);
                        w.Write(p.Selected);
                        w.Write((Int32)slots.Count());
                        foreach(CMachineSnapshot snapshot in slots)
                        {
                            w.Write((Int32)snapshot.Index);
                            snapshot.WriteProperty(p, w);
                        }
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
        private CMachineStateData _loadedState;
        private bool loading = false;
        public CMachineStateData MachineState
        {
            get
            {
                return new CMachineStateData(this);
            }
            set
            {
                try
                {
                    loading = true;
                    _loadedState = value;
                }
                catch(Exception e)
                {
                    _loadedState = new CMachineStateData(this);
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
            if (_loadedState.data.Length == 0) return;

            if (_loadedState.version > 1) throw new Exception("Version mismatch");

            MemoryStream stream = new MemoryStream(_loadedState.data);
            BinaryReader r = new BinaryReader(stream);

            // Options
            SelectNewMachines = r.ReadBoolean();
            CaptureOnSlotChange = r.ReadBoolean();
            RestoreOnSlotChange = r.ReadBoolean();
            RestoreOnSongLoad = r.ReadBoolean();
            RestoreOnStop = r.ReadBoolean();

            // Slot data (CMachineSnapshot.WriteData() x 128)
            for (int i = 0; i < 128; i++)
            {
                _slots[i].ReadData(r);
            }

            // number of saved states
            Int32 numStates = r.ReadInt32();
            for(int n = 0; n < numStates; n++)
            {
                string name = r.ReadString(); // State name
                string dllname = r.ReadString(); // Machine DLL name
                try
                {
                    // Should be one and only one state matching both name and dllname. Exception if not.
                    CMachineState s = States.Single(x => x.Machine.Name == name && x.Machine.DLL.Name == dllname);

                    Int32 count = r.ReadInt32(); // number of properties saved
                    for (Int32 i = 0; i < count; i++)
                    {
                        name = r.ReadString(); // Property name
                        int? track = r.ReadInt32(); // Property track (-1 if null)
                        if (track < 0) track = null;

                        // Should be one and only one property matching name and track. Exception if not.
                        IPropertyState ps = s.AllProperties.Single(x => x.Name == name && x.Track == track);

                        ps.Selected = r.ReadBoolean(); //Property selected

                        Int32 numslots = r.ReadInt32(); // number of saved snapshot values
                        for (int j = 0; j < numslots; j++)
                        {
                            Int32 slot = r.ReadInt32(); // snapshot index
                            _slots[slot].ReadProperty(ps, r); // Snapshot data (CMachineSnapshot.WriteProperty())
                        }
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                    return;
                }
            }

            OnPropertyChanged("State");
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
                CMachineState ms = new CMachineState(this, m);
                foreach (IPropertyState s in ms.AllProperties)
                {
                    s.SelChanged += OnSelChanged;
                    s.Selected = SelectNewMachines;
                    AllProperties.Add(s);
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

        private void OnSelChanged(object sender, StateChangedEventArgs e)
        {
            OnPropertyChanged("SelectionInfo");
        }

        private void OnMachineRemoved(IMachine m)
        {
            if (m != host.Machine)
            {
                CMachineState s = States[States.FindIndex(x => x.Machine == m)];
                States.Remove(s);
                VM.RemoveState(s);
            }
        }
        #endregion events

        #region Global Parameters
        // Global params
        int _slot;
        [ParameterDecl(IsStateless = false, MinValue = 0, MaxValue = 127, DefValue = 1, Description = "Active slot", Name = "Slot")]
        public int Slot
        {
            get => _slot;
            set
            {
                if (value < 0 || value > 127) return;

                if (_slot != value)
                {
                    OnSlotChanging();

                    _slot = value;

                    // This is to update the parameter if the slot change comes from the combo
                    if (SlotParam != null && SlotParam.GetValue(0) != _slot)
                    {
                        Application.Current.Dispatcher.BeginInvoke((Action)(() => { SlotParam.SetValue(0, _slot); }), DispatcherPriority.Send);
                    }

                    OnSlotChanged();
                }
            }
        }

        #endregion Global Parameters

        #region Commands

        internal void Capture()
        {
            CurrentSlot.Capture();
            OnPropertyChanged("State");
        }

        internal void CaptureMissing()
        {
            CurrentSlot.CaptureMissing();
            OnPropertyChanged("State");
        }

        internal void Restore()
        {
            CMachineSnapshot s = CurrentSlot;
            if(s.HasData)
            {
                Application.Current.Dispatcher.BeginInvoke((Action)(() => { s.Restore(); }), DispatcherPriority.Send);
            }
        }

        internal void Purge()
        {
            CurrentSlot.Purge();
            OnPropertyChanged("State");
        }

        internal void Clear()
        {
            CurrentSlot.Clear();
            OnPropertyChanged("State");
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
        internal void OnSlotChanged()
        {
            if (RestoreOnSlotChange && !loading)
            {
                Restore();
            }

            OnPropertyChanged("State");
        }

        #endregion Commands

        public event PropertyChangedEventHandler PropertyChanged;
        public virtual void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class Misc
    {
        // Byte count to formatted string solution - https://stackoverflow.com/a/48467634
        private static string[] suffixes = new[] { "b", "K", "M", "G", "T", "P" };
        public static string ToSize(double number, int precision = 2)
        {
            // unit is the number of bytes
            const double unit = 1024;

            // suffix counter
            int i = 0;

            // as long as we're bigger than a unit, keep going
            while (number > unit)
            {
                number /= unit;
                i++;
            }

            // apply precision and current suffix
            return Math.Round(number, precision) + suffixes[i];
        }
    }
}