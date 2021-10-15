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
using System.Reflection;

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
                if (value != null && value != CurrentSlot.Name)
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
                if (_selectNewMachines != value)
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

        // This is the mapping of UI actions to MIDI events
        public Dictionary<Action, CMidiEvent> MidiMap { get; private set; }

        // This is the mechanism to trigger UI actions in response to MIDI events
        private Dictionary<Action, UInt32 /*code*/> _midiMapping;

        #region IBuzzMachine

        public CMachine(IBuzzMachineHost host)
        {
            this.host = host;
            MidiMap = new Dictionary<Action, CMidiEvent>();
            _midiMapping = new Dictionary<Action, UInt32>();

            _slots = new List<CMachineSnapshot>();
            for (int i = 0; i < 128; i++)
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

        // Class to hold whatever needs saving to the song
        public class CMachineStateData
        {
            public CMachineStateData()
            {
            }

            public Byte version = 1;
            public byte[] data;
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
                return GetStateData();
            }
            set
            {
                try
                {
                    loading = true;
                    _loadedState = value;
                }
                catch (Exception e)
                {
                    _loadedState = new CMachineStateData();
                }
            }
        }

        public void Stop()
        {
            if (RestoreOnStop)
            {
                Restore();
            }
        }

        internal void MapCommand(string name, bool specific)
        {
            // Find the command
            object target;
            Type targetType;
            MethodInfo method;
            if (specific)
            {
                targetType = CurrentSlot.GetType();
                method = targetType.GetMethod(name, BindingFlags.Public | BindingFlags.Instance);
                target = CurrentSlot;
            }
            else
            {
                targetType = this.GetType();
                method = targetType.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance);
                target = this;
            }
            Action a = (Action)Delegate.CreateDelegate(typeof(Action), target, method);

            // Retrieve the mapping or use defaults
            CMidiEvent e;
            if(MidiMap.ContainsKey(a))
            {
                e = MidiMap[a];
            }
            else
            {
                e = new CMidiEvent();
            }

            // Show mapping dialog
            CMappingDialog d = new CMappingDialog(name, e);
            bool? result = d.ShowDialog();

            // Add/update mapping if necessary
            if(result == true)
            {
                e.Message = (CMidiEvent.MessageType) d.EventType;
                e.Channel = d.Channel;
                e.Primary = d.Primary;
                e.Secondary = d.Secondary;
                MidiMap[a] = e;
                _midiMapping[a] = e.Encode();
            }
        }

        /* FILE STRUCTURE
         * 
         * SelectNewMachines
         * CaptureOnSlotChange
         * RestoreOnSlotChange
         * RestoreOnSongLoad
         * RestoreOnStop
         * Slot data (CMachineSnapshot.WriteData() x 128)
         * number of saved MIDI mappings
         * * method name
         * * slot index or -1 for machine
         * * midi event data (CMidiEvent.WriteData())
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

        private CMachineStateData GetStateData()
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter w = new BinaryWriter(stream);

            // Options
            w.Write(SelectNewMachines);
            w.Write(CaptureOnSlotChange);
            w.Write(RestoreOnSlotChange);
            w.Write(RestoreOnSongLoad);
            w.Write(RestoreOnStop);

            // Save slot data
            for (int i = 0; i < 128; i++)
            {
                _slots[i].WriteData(w);
            }

            // Save MIDI mapping
            w.Write(MidiMap.Count());
            foreach (KeyValuePair<Action, CMidiEvent> item in MidiMap)
            {
                Action a = item.Key;

                // if action belongs to a snapshot, we need to save the index
                int slot = -1;
                switch (a.Target.GetType().Name)
                {
                    case "CMachineSnapshot":
                        slot = (a.Target as CMachineSnapshot).Index;
                        break;

                    default:
                        break;
                }

                w.Write(a.Method.Name);
                w.Write(slot);
                item.Value.WriteData(w);
            }

            // Build a list of states to save by finding which are referred to by snapshots
            List<CMachineState> saveStates = new List<CMachineState>();
            foreach (CMachineState s in States)
            {
                if (_slots.Exists(x => x.ContainsMachine(s)))
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
                foreach (CPropertyBase ps in s.AllProperties)
                {
                    List<CMachineSnapshot> slots = _slots.Where(x => x.ContainsProperty(ps)).ToList();
                    if (slots.Count() > 0 || ps.Selected)
                    {
                        saveProperties[ps] = slots;
                    }
                }
                w.Write((Int32)saveProperties.Count());
                foreach (KeyValuePair<CPropertyBase, List<CMachineSnapshot>> item in saveProperties)
                {
                    CPropertyBase p = item.Key;
                    List<CMachineSnapshot> slots = item.Value;
                    w.Write(p.Name);
                    w.Write(p.Track ?? -1);
                    w.Write(p.Selected);
                    w.Write((Int32)slots.Count());
                    foreach (CMachineSnapshot snapshot in slots)
                    {
                        w.Write((Int32)snapshot.Index);
                        snapshot.WriteProperty(p, w);
                    }
                }
            }

            CMachineStateData data = new CMachineStateData() { data = stream.ToArray() };

            return data;
        }

        private void RestoreLoadedData()
        {
            if (_loadedState == null) return;

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

            // MIDI map
            Int32 numMappings = r.ReadInt32();
            for (int n = 0; n < numMappings; n++)
            {
                string action = r.ReadString();
                int slot = r.ReadInt32();
                CMidiEvent e = new CMidiEvent();
                e.ReadData(r);

                // Restore mapping settings
                object target;
                Type targetType;
                MethodInfo method;
                if (slot < 0) // machine action
                {
                    targetType = typeof(CMachine);
                    method = targetType.GetMethod(action, BindingFlags.NonPublic | BindingFlags.Instance);
                    target = this;
                }
                else // snapshot action
                {
                    targetType = typeof(CMachineSnapshot);
                    method = targetType.GetMethod(action, BindingFlags.Public | BindingFlags.Instance);
                    target = _slots[slot];
                }

                // Restore settings
                Action a = (Action)Delegate.CreateDelegate(typeof(Action), target, method);
                MidiMap[a] = e; // settings
                _midiMapping[a] = e.Encode(); // mapping
            }

            // number of saved states
            Int32 numStates = r.ReadInt32();
            for (int n = 0; n < numStates; n++)
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

            if (RestoreOnSongLoad)
            {
                Restore();
            }
        }

        public void MidiNote(int channel, int note, int velocity)
        {
            // This note on this channel
            UInt32 c1 = (UInt32)(((Byte)CMidiEvent.MessageType.Note_On << 24) | (128 /*v=any*/ << 16) | (note << 8) | channel);

            // This note on any channel
            UInt32 c2 = (UInt32)(((Byte)CMidiEvent.MessageType.Note_On << 24) | (128 /*v=any*/ << 16) | (note << 8) | 16 /*c=any*/);

            if (velocity == 0)
            {
                c1 = c1 & 0xFFFFFF | ((Byte)CMidiEvent.MessageType.Note_Off << 24);
            }

            // Fire off matching actions
            foreach (KeyValuePair<Action, UInt32> item in _midiMapping.Where(x => x.Value == c1 || x.Value == c2))
            {
                item.Key();
            }
        }

        public void MidiControlChange(int ctrl, int channel, int value)
        {
            // This controller on this channel, with this value
            UInt32 c1 = (UInt32)(((Byte)CMidiEvent.MessageType.Controller << 24) | (value << 16) | (ctrl << 8) | channel);

            // This controller on this channel, with any value
            UInt32 c2 = (UInt32)(((Byte)CMidiEvent.MessageType.Controller << 24) | (128/*v=any*/ << 16) | (ctrl << 8) | channel);

            // This controller on any channel, with this value
            UInt32 c3 = (UInt32)(((Byte)CMidiEvent.MessageType.Controller << 24) | (value << 16) | (ctrl << 8) | 16 /*c=any*/);

            // This controller on any channel, with any value
            UInt32 c4 = (UInt32)(((Byte)CMidiEvent.MessageType.Controller << 24) | (128/*v=any*/ << 16) | (ctrl << 8) | 16 /*c=any*/);

            // Fire off matching actions
            foreach (KeyValuePair<Action, UInt32> item in _midiMapping.Where(x => x.Value == c1 || x.Value == c2 || x.Value == c3 || x.Value == c4))
            {
                item.Key();
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
                s.Restore();
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

    public class CMidiEvent
    {
        public CMidiEvent()
        {
            Channel = 16;
            Message = MessageType.Undefined;
            Primary = 128;
            Secondary = 128;
        }

        public Byte Channel { get; set; } // 16 = Any

        public enum MessageType { Undefined = 0, Note_On, Note_Off, Controller }
        public MessageType Message { get; set; }
        
        public Byte Primary { get; set; } // Note or CC number. 128 = undefined
        
        public Byte Secondary { get; set; } // Velocity or value. 128 = undefined

        public UInt32 Encode()
        {
            return (UInt32) (((Byte)Message << 24) | (Secondary << 16) | (Primary << 8) | Channel);
        }

        internal void ReadData(BinaryReader r)
        {
            Channel = r.ReadByte();
            Message = (MessageType) r.ReadByte();
            Primary = r.ReadByte();
            Secondary = r.ReadByte();
        }

        internal void WriteData(BinaryWriter w)
        {
            w.Write(Channel);
            w.Write((Byte) Message);
            w.Write(Primary);
            w.Write(Secondary);
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
