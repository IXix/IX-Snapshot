using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
using System.Threading;

namespace Snapshot
{
    public class CMachineGUIFactory : IMachineGUIFactory
    {
        public IMachineGUI CreateGUI(IMachineGUIHost host) { return new GUI(); }
    }

    [MachineDecl(Name = "IX Snapshot 1.5", ShortName = "Snapshot", Author = "IX", MaxTracks = 0, InputCount = 0, OutputCount = 0)]
    public class CMachine : IBuzzMachine, INotifyPropertyChanged
    {
        private readonly IBuzzMachineHost host;

        internal Stopwatch timer;
        internal double workTimeStamp;

        private readonly Thread WorkThread;
        private int workFlag = 0;

        private static ManualResetEvent processChanges = new ManualResetEvent(false);

        public static readonly string[] NoteNames = new string[128];

        private IMachine ThisMachine { get; set; }
        private IParameter SlotParam { get; set; }

        public ObservableCollection<CMachineState> States { get; private set; }

        public CSnapshotMachineVM VM { get; }

        public HashSet<CPropertyBase> AllProperties { get; private set; }

        internal List<CParamChange> paramChanges;
        internal List<CAttribChange> attribChanges;

        private readonly ObservableCollection<CMachineSnapshot> _slots;
        public ObservableCollection<CMachineSnapshot> Slots => _slots;

        public CMachineSnapshot CurrentSlot => _slots[_slot];

        public CMachineSnapshot SlotA => _slots[_slotA];
        public int SelA
        {
            get { return _slotA; }
            set
            {
                if (_slotA != value)
                {
                    _slotA = value;
                    OnPropertyChanged("SlotA");
                    OnPropertyChanged("FilterM");
                }
            }
        }

        public CMachineSnapshot SlotB => _slots[_slotB];
        public int SelB
        {
            get { return _slotB; }
            set
            {
                if (_slotB != value)
                {
                    _slotB = value;
                    OnPropertyChanged("SlotB");
                    OnPropertyChanged("FilterM");
                }
            }
        }

        public string SlotName
        {
            get => CurrentSlot.Name;
            set
            {
                if (value != null && value != CurrentSlot.Name)
                {
                    CurrentSlot.Name = value;
                    OnPropertyChanged("SlotName");
                    if (CurrentSlot == SlotA)
                        OnPropertyChanged("SlotNameA");
                    if (CurrentSlot == SlotB)
                        OnPropertyChanged("SlotNameB");
                }
            }
        }

        public string SlotNameA
        {
            get => SlotA.Name;
            set
            {
                if (value != null && value != SlotA.Name)
                {
                    SlotA.Name = value;
                    OnPropertyChanged("SlotNameA");
                    if (SlotA == CurrentSlot)
                        OnPropertyChanged("SlotName");
                    if (SlotA == SlotB)
                        OnPropertyChanged("SlotNameB");
                }
            }
        }

        public string SlotNameB
        {
            get => SlotB.Name;
            set
            {
                if (value != null && value != SlotB.Name)
                {
                    SlotB.Name = value;
                    OnPropertyChanged("SlotNameB");
                    if (SlotB == CurrentSlot)
                        OnPropertyChanged("SlotName");
                    if (SlotB == SlotA)
                        OnPropertyChanged("SlotNameA");
                }
            }
        }

        public string Info
        {
            get
            {
                string txt = string.Format("{0} of {1} properties selected\n", SelCount, AllProperties.Count);

                txt += "\n";

                txt += string.Format("Active slot is {0}: {1} ({2})", CurrentSlot.Index, CurrentSlot.Name, Misc.ToSize(CurrentSlot.Size));
                if (CurrentSlot.Notes != "")
                    txt += string.Format(" - \"{0}\"", CurrentSlot.Notes);
                txt += "\n";

                txt += string.Format(
                    "{0} stored\n" +
                    "{1} selected but not stored\n" +
                    "{2} stored but not selected\n" +
                    "{3} stored for missing properties\n",
                    StoredCount,
                    MissingCount,
                    RedundantCount,
                    DeletedCount
                    );

                txt += "\n";

                txt += "Used slots:\n";
                foreach(CMachineSnapshot slot in Slots.Where(x => x.HasData))
                {
                    txt += string.Format("{0}: {1} ({2})", slot.Index, slot.Name, Misc.ToSize(slot.Size));
                    if (slot.Notes != "")
                        txt += string.Format(" - \"{0}\"", slot.Notes);
                    txt += "\n";
                }

                txt += string.Format("Total size: {0}", Misc.ToSize(TotalSize));

                return txt;
            }
        }

        private bool _confirmClear;
        public bool ConfirmClear
        {
            get => _confirmClear;
            set
            {
                if (_confirmClear != value)
                {
                    _confirmClear = value;
                    OnPropertyChanged("ConfirmClear");
                }
            }
        }

        private bool _selectionFollowsSlot;
        public bool SelectionFollowsSlot
        {
            get => _selectionFollowsSlot;
            set
            {
                if (_selectionFollowsSlot != value)
                {
                    _selectionFollowsSlot = value;

                    PushSelection();

                    OnPropertyChanged("SelectionFollowsSlot");
                    OnPropertyChanged("Selection");
                }
            }
        }

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
        public int SelCount => Selection.Count;
        public int SelCountM => SelectionM.Count;

        // How many properties have been captured
        public int StoredCount => CurrentSlot.StoredCount;

        // How many properties are stored that aren't selected
        public int RedundantCount => CurrentSlot.RedundantCount;
        public int RedundantCountA => SlotA.RedundantCount;
        public int RedundantCountB => SlotB.RedundantCount;

        // How many properties are stored but are inactive (machine deleted)
        public int DeletedCount => CurrentSlot.DeletedCount;

        // How many selected properties have not been captured
        public int MissingCount => AllProperties.Where(x => x.Checked == true && x.Active).Except(CurrentSlot.StoredProperties).Count();
        public int MissingCountA => AllProperties.Where(x => x.Checked_M == true && x.Active).Except(SlotA.StoredProperties).Count();
        public int MissingCountB => AllProperties.Where(x => x.Checked_M == true && x.Active).Except(SlotB.StoredProperties).Count();

        public string Name
        {
            get => host.Machine.Name;
            set => host.Machine.Name = value;
        }

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

        internal void UpdateSizeInfo()
        {
            foreach (CMachineState s in States.Where(x => x.DataState != null))
            {
                s.DataState.UpdateSize();
            }
        }

        public bool SlotHasData => CurrentSlot.StoredCount > 0;

        // This is the mapping of UI actions to MIDI events
        public ObservableCollection<CMidiTargetInfo> MidiMap { get; private set; }

        public CMidiBindingInfo MidiInfo;

        // This is the mechanism to trigger UI actions in response to MIDI events
        private readonly Dictionary<UInt32 /*code*/, HashSet<CMidiAction>> _midiMapping;

        #region IBuzzMachine

        public CMachine(IBuzzMachineHost host)
        {
            this.host = host;
            timer = new Stopwatch();
            m_selection = new CPropertySelection(this, true);
            m_selectionM = new CPropertySelection(this, true);

            // Build note list if necessary
            if (NoteNames[0] == null)
            {
                string[] notes = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
                for (int i = 0; i < 128; i++)
                {
                    NoteNames[i] = string.Format("{0}: {1}{2}", i + 1, notes[i % 12], i / 12);
                }
            }

            WorkThread = new Thread(SendChanges)
            {
                Name = "Snapshot:WorkThread",
                Priority = ThreadPriority.Normal // ThreadPriority.Highest
            };
            WorkThread.Start();

            MidiMap = new ObservableCollection<CMidiTargetInfo>();
            _midiMapping = new Dictionary<uint, HashSet<CMidiAction>>();
            _confirmClear = true;

            changeLock = new object();
            paramChanges = new List<CParamChange>();
            attribChanges = new List<CAttribChange>();

            _slots = new ObservableCollection<CMachineSnapshot>();
            _slot = _slotA = 0;
            _slotB = 1;
            for (int i = 0; i < 128; i++)
            {
                _slots.Add(new CMachineSnapshot(this, i));
            }

            States = new ObservableCollection<CMachineState>();
            AllProperties = new HashSet<CPropertyBase>();
            VM = new CSnapshotMachineVM(this);
            MidiInfo = new CMidiBindingInfo();

            ReadOnlyCollection<IMachine> machines = Global.Buzz.Song.Machines;
            foreach (IMachine m in machines)
            {
                OnMachineAdded(m);
            }

            Global.Buzz.Song.MachineAdded += (m) => { OnMachineAdded(m); };
            Global.Buzz.Song.MachineRemoved += (m) => { OnMachineRemoved(m); };
        }

        public IEnumerable<IMenuItem> Commands
        {
            get
            {
                IEnumerable<CMachineSnapshot> s = _slots.Where(x => x.HasData);
                if (s.Count() == 0)
                {
                    yield return new MenuItemVM() { Text = "<no slot data>" };
                    yield break;
                }

                // Append non-empty slots to menu
                // Selecting the menu item will make the relevant slot current and restore it
                foreach (CMachineSnapshot slot in _slots.Where(x => x.HasData))
                {
                    yield return new MenuItemVM()
                    {
                        Text = slot.Name,
                        Command = new SimpleCommand()
                        {
                            CanExecuteDelegate = p => true,
                            ExecuteDelegate = p => { Slot = slot.Index; ForceRestore(); }
                        }
                    };
                }
            }
        }


        // Class to hold whatever needs saving to the song
        public class CMachineStateData
        {
            public CMachineStateData()
            {
            }

            public static Byte currentVersion = 5;
            public Byte version = currentVersion; // Current file version
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
                catch (Exception)
                {
                    _loadedState = new CMachineStateData();
                }
            }
        }

        public Byte LoadVersion
        {
            get
            {
                try
                {
                    return _loadedState.version;

                }
                catch
                {
                    return 0;
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

        private void SendChanges(object param)
        {
            if (!timer.IsRunning) timer.Start();

            while (true)
            {
                if (Interlocked.CompareExchange(ref workFlag, 1, 0) != 0)
                {
                    break;
                }

                // Wait for flag
                processChanges.WaitOne();

                double totalsecs = timer.Elapsed.TotalSeconds;
                double seconds = totalsecs - workTimeStamp;
                workTimeStamp = totalsecs;

                int samples = (int)Math.Round(host.MasterInfo.SamplesPerSec * seconds);
                lock (changeLock)
                {
                    HashSet<IMachine> affectedMachines = new HashSet<IMachine>();

                    // Params
                    foreach (CParamChange p in paramChanges)
                    {
                        p.Work(samples);
                        affectedMachines.Add(p.Machine);
                    }

                    // Attributes
                    foreach (CAttribChange a in attribChanges)
                    {
                        a.Work();
                    }

                    foreach (IMachine m in affectedMachines)
                    {
                        m.SendControlChanges();
                    }
                }

                Cleanup(null);

                processChanges.Reset();

                Interlocked.Exchange(ref workFlag, 0);
            }
        }

        private void Cleanup(object param)
        // Remove spent items from the change lists
        {
            lock (changeLock)
            {
                _ = attribChanges.RemoveAll(x => x.Finished);
                _ = paramChanges.RemoveAll(x => x.Finished);
            }
        }

        
        public bool Work(Sample[] output, int n, WorkModes mode)
        {
            if(host.SubTickInfo.PosInSubTick == 0)
            {
                processChanges.Set();
            }

            return false;
        }
        

        internal void ClearPendingChanges()
        {
            attribChanges.Clear();
            paramChanges.Clear();
        }

        internal void RegisterAttribChange(IAttribute attr, int value, bool clearPending = false)
        {
            // Clear any pending changes for same attribute
            if (clearPending)
                attribChanges.RemoveAll(x => x.Attribute == attr);

            attribChanges.Add(new CAttribChange(attr, value));
        }

        internal void RegisterParamChange(CParameterState ps, int track, int value, bool clearPending = false)
        {
            IMachine machine = ps.Machine;
            IParameter param = ps.Parameter;

            // Work up the chain to find a non-null value for smoothing
            // ALthough CPropertyBase exposes properties for these, it's slightly more efficient to do this instead
            // Using the properties would cost three times as many loops.
            CPropertyBase p = ps;
            int? count = null;
            int? units = null;
            int? shape = null;
            while(p != null)
            {
                units = units?? p.SmoothingUnits;
                count = count?? p.SmoothingCount;
                shape = shape ?? p.SmoothingShape;
                p = p.Parent;
            }

            // If still null, use machine level values
            units = units ?? SmoothingUnits;
            count = count ?? SmoothingCount;
            shape = shape ?? SmoothingShape;

            // Clear any pending changes for same param
            if (clearPending)
                paramChanges.RemoveAll(x => x.Parameter == param && x.track == track);

            double spu = 0;
            switch (units)
            {
                case 0: // Ticks
                    spu = host.MasterInfo.SamplesPerTick; break;

                case 1: // Beats
                    spu = host.MasterInfo.SamplesPerTick * host.MasterInfo.TicksPerBeat; break;

                case 2: // Milliseconds
                    spu = host.MasterInfo.SamplesPerSec * 0.001; break;

                case 3: // Seconds
                    spu = host.MasterInfo.SamplesPerSec; break;

                case 4: // Minutes
                    spu = host.MasterInfo.SamplesPerSec * 60; break;

                default:
                    throw new Exception("Unexpected case for time units.");

            }
            int duration = (int)Math.Round((double) count * spu);

            paramChanges.Add(new CParamChange(machine, param, track, value, duration, (int) shape));
        }

        public void PushSelection()
        {
            foreach (CPropertyBase p in AllProperties)
            {
                p.Checked = Selection.Contains(p);
            }
            OnPropertyChanged("Selection");
        }

        public void SelectAll()
        {
            foreach (CPropertyBase s in AllProperties)
            {
                s.Checked = true;
            }
            OnPropertyChanged("Selection");
        }

        public void SelectNone()
        {
            foreach (CPropertyBase s in AllProperties)
            {
                s.Checked = false;
            }
            OnPropertyChanged("Selection");
        }

        public void SelectStored()
        {
            foreach (CPropertyBase s in AllProperties)
            {
                s.Checked = s.GotValue;
            }
            OnPropertyChanged("Selection");
        }

        public void SelectInvert()
        {
            foreach (CPropertyBase s in AllProperties)
            {
                s.Checked = !s.Checked;
            }
            OnPropertyChanged("Selection");
        }

        public void PushSelectionM()
        {
            foreach (CPropertyBase p in AllProperties)
            {
                p.Checked_M = SelectionM.Contains(p);
            }
            OnPropertyChanged("SelectionM");
        }

        public void SelectAll_M()
        {
            foreach (CPropertyBase s in AllProperties)
            {
                s.Checked_M = true;
            }
            OnPropertyChanged("SelectionM");
        }

        public void SelectNone_M()
        {
            foreach (CPropertyBase s in AllProperties)
            {
                s.Checked_M = false;
            }
            OnPropertyChanged("SelectionM");
        }

        public void SelectInvert_M()
        {
            foreach (CPropertyBase s in AllProperties)
            {
                s.Checked_M = !s.Checked_M;
            }
            OnPropertyChanged("SelectionM");
        }

        public void SelectStoredA()
        {
            foreach (CPropertyBase s in AllProperties)
            {
                s.Checked_M = SlotA.ContainsProperty(s);
            }
            OnPropertyChanged("SelectionM");
        }

        public void SelectStoredB()
        {
            foreach (CPropertyBase s in AllProperties)
            {
                s.Checked_M = SlotB.ContainsProperty(s);
            }
            OnPropertyChanged("SelectionM");
        }

        internal bool Confirm(string title, string msg, bool force=false)
        {
            if (ConfirmClear || force)
            {
                MessageBoxResult result = MessageBox.Show(msg, title, MessageBoxButton.YesNo);
                if (result == MessageBoxResult.No)
                {
                    return false;
                }
            }
            return true;
        }

        private void CopySelectedProperties(CMachineSnapshot src, CMachineSnapshot dest)
        {
            dest.CopyFrom(SelectionM, src);
        }

        public void CopyAtoB()
        {
            string msg = string.Format("Copy {0} properties from {1} to {2}?", SelectionM.Count, SlotA.Name, SlotB.Name);
            if (Confirm("Confirm", msg))
            {
                CopySelectedProperties(SlotA, SlotB);
                OnPropertyChanged("State");
            }
        }

        public void CopyBtoA()
        {
            string msg = string.Format("Copy {0} properties from {1} to {2}?", SelectionM.Count, SlotB.Name, SlotA.Name);
            if (Confirm("Confirm", msg))
            {
                CopySelectedProperties(SlotB, SlotA);
                OnPropertyChanged("State");
            }
        }

        internal readonly CPropertySelection m_selection;
        public HashSet<CPropertyBase> Selection
        {
            get => _selectionFollowsSlot ? CurrentSlot.Selection : m_selection.SelectedProperties;
        }

        internal readonly CPropertySelection m_selectionM;
        public HashSet<CPropertyBase> SelectionM
        {
            get => m_selectionM.SelectedProperties;
        }

        public void CaptureA()
        {
            SlotA.Capture(SelectionM, false);
            OnPropertyChanged("State");
        }

        public void CaptureMissingA()
        {
            HashSet<CPropertyBase> targets = new HashSet<CPropertyBase>(SelectionM);
            targets.RemoveWhere(x => SlotA.ContainsProperty(x) == true);
            SlotA.Capture(targets, false);
            OnPropertyChanged("State");
        }

        public void PurgeA()
        {
            string msg = string.Format("Discard {0} stored properties from {1}?", SlotA.RedundantCount_M, SlotA.Name);
            if (Confirm("Confirm purge", msg))
            {
                SlotA.Purge(SelectionM, false);
                OnPropertyChanged("State");
            }
        }

        public void ClearSelectedA()
        {
            string msg = string.Format("Discard {0} stored properties from {1}?", SlotA.SelectedCount_M, SlotA.Name);
            if (Confirm("Confirm clear", msg))
            {
                SlotA.Remove(SelectionM);
                OnPropertyChanged("State");
            }
        }

        public void ClearA()
        {
            string msg = string.Format("Discard all stored properties from {0}?", SlotA.Name);
            if (Confirm("Confirm clear", msg))
            {
                SlotA.Clear(false);
                OnPropertyChanged("State");
            }
        }

        public void RestoreA()
        {
            SlotA.Restore();
        }

        public void CaptureB()
        {
            SlotB.Capture(SelectionM, false);
            OnPropertyChanged("State");
        }

        public void CaptureMissingB()
        {
            HashSet<CPropertyBase> targets = new HashSet<CPropertyBase>(SelectionM);
            targets.RemoveWhere(x => SlotB.ContainsProperty(x) == true);
            SlotB.Capture(targets, false);
            OnPropertyChanged("State");
        }

        public void PurgeB()
        {
            string msg = string.Format("Discard {0} stored properties from {1}?", SlotB.RedundantCount_M, SlotB.Name);
            if (Confirm("Confirm purge", msg))
            {
                SlotB.Purge(SelectionM, false);
                OnPropertyChanged("State");
            }
        }

        public void ClearSelectedB()
        {
            string msg = string.Format("Discard {0} stored properties from {1}?", SlotB.SelectedCount_M, SlotB.Name);
            if (Confirm("Confirm clear", msg))
            {
                SlotB.Remove(SelectionM);
                OnPropertyChanged("State");
            }
        }

        public void ClearB()
        {
            if (Confirm("Confirm clear", "Discard all stored properties from right?"))
            {
                SlotB.Clear(false);
                OnPropertyChanged("State");
            }
        }

        public void RestoreB()
        {
            SlotB.Restore();
        }

        internal List<CMidiTargetInfo> FindDuplicateMappings(CMidiEventSettings settings)
        {
            return MidiMap.Where(item => item.settings.CheckConflict(settings)).ToList();
        }

        internal void RemoveMapping(CMidiTargetInfo info)
        {
            if (MidiMap.Contains(info))
            {
                UInt32 code = info.settings.Encode();

                _ = _midiMapping[code].Remove(info.action);
                if (_midiMapping[code].Count == 0)
                {
                    _ = _midiMapping.Remove(code);
                }

                info.settings.Reset();

                MidiMap.Remove(info);

                if (info.index < 0) // Machine
                {
                    MidiInfo.Update(info);
                }
                else // Slot
                {
                    CMachineSnapshot slot = info.target as CMachineSnapshot;
                    slot.MidiInfo.Update(info);
                }
            }
        }

        internal void RemoveMappings(List<CMidiTargetInfo> conflicts)
        {
            foreach(CMidiTargetInfo target in conflicts)
            {
                RemoveMapping(target);
            }

            OnPropertyChanged("MidiMap");
        }

        internal void MapCommand(CMidiTargetInfo info)
        {
            CMidiEventSettings e = info.settings;
            UInt32? prevCode = null;
            if(info.action != null)
            {
                prevCode = e.Encode();
            }

            // Show mapping dialog
            _mappingDialog = new CMappingDialog(this, info);
            bool? result = _mappingDialog.ShowDialog();

            bool removePrev;

            // Add/update mapping if necessary
            if (result == true)
            {
                if (e.Message > 0)
                {
                    if (prevCode == null) // New mapping
                    {
                        MidiMap.Add(info);
                    }

                    UInt32 code = e.Encode();

                    // Flag to remove old mapping if necessary
                    removePrev = prevCode != null && code != prevCode;

                    if (e.StoreSelection)
                    {
                        e.Selection = new CPropertySelection(this, Selection);
                    }
                    else
                    {
                        e.Selection = null;
                    }

                    info.SetAction();

                    if (_midiMapping.ContainsKey(code))
                    {
                        _ = _midiMapping[code].Add(info.action);
                    }
                    else
                    {
                        _ = _midiMapping[code] = new HashSet<CMidiAction>() { info.action };
                    }
                }
                else // Undefined - remove mapping
                {
                    _ = MidiMap.Remove(info);
                    removePrev = true;
                }

                if (removePrev && prevCode != null)
                {
                    if (_midiMapping.ContainsKey((UInt32)prevCode))
                    {
                        _ = _midiMapping[(UInt32)prevCode].Remove(info.action);
                        if (_midiMapping[(UInt32)prevCode].Count == 0)
                        {
                            _ = _midiMapping.Remove((UInt32)prevCode);
                        }
                    }
                }

                if (info.index < 0) // Machine
                {
                    MidiInfo.Update(info);
                }
                else // Slot
                {
                    CMachineSnapshot slot = info.target as CMachineSnapshot;
                    slot.MidiInfo.Update(info);
                }
            }

            // Reset these
            MappingDialogSettings = null;
            _mappingDialog = null;

            OnPropertyChanged("MidiMap");
        }

        internal void MapCommand(string command, bool specific)
        {
            int index = specific ? CurrentSlot.Index: -1;

            // Retrieve the mapping or use defaults
            CMidiTargetInfo key;
            try
            {
                key = MidiMap.First(x => x.index == index && x.command == command);
            }
            catch (InvalidOperationException) // Not in collection
            {
                key = new CMidiTargetInfo(index, command, this);
            }

            MapCommand(key);
        }

        /* FILE STRUCTURE
         * 
         * SelectNewMachines
         * CaptureOnSlotChange
         * RestoreOnSlotChange
         * RestoreOnSongLoad
         * RestoreOnStop
         * SelectionFollowsSlot
         * ConfirmClear
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
            w.Write(SelectionFollowsSlot); // new in v2
            w.Write(ConfirmClear);         // ^^

            // Save slot data
            for (int i = 0; i < 128; i++)
            {
                _slots[i].WriteData(w);
            }

            // Save MIDI mapping
            w.Write(MidiMap.Count());
            foreach (CMidiTargetInfo info in MidiMap)
            {
                w.Write(info.command);
                w.Write(info.index);
                info.settings.WriteData(w);
            }

            // Build a set of properties that need saving.
            // Any that are selected somewhere, have non-inherited smoothing or stored in a slot.
            HashSet<CPropertyBase> propertySet = new HashSet<CPropertyBase>();
            propertySet.UnionWith(m_selection.SelectedProperties);
            propertySet.UnionWith(m_selectionM.SelectedProperties);
            foreach (CMachineSnapshot slot in _slots)
            {
                propertySet.UnionWith(slot.Selection);
                propertySet.UnionWith(slot.StoredProperties);
            }
            propertySet.UnionWith(AllProperties.Where(x => x.HasSmoothing));

            propertySet.RemoveWhere(x => x.Active == false); // Avoid saving redundant states

            // Separate into dictionary of machines and their properties
            Dictionary<CMachineState, HashSet<CPropertyBase>> saveStates = new Dictionary<CMachineState, HashSet<CPropertyBase>>();

            // Need to account for non-property smoothing. Stuff these in first to keep things simple
            foreach(CMachineState s in States.Where(x => x.HasSmoothing || x.ChildHasSmoothing))
            {
                saveStates[s] = new HashSet<CPropertyBase>();
            }

            // Associate machine states and their properties in the dictionary
            foreach (CPropertyBase p in propertySet)
            {
                if (p is CMachineState || p is CPropertyStateGroup || p is CTrackPropertyStateGroup)
                {
                    throw new Exception("Group!"); // shouldn't happen
                }

                if (saveStates.ContainsKey(p.ParentMachine))
                {
                    _ = saveStates[p.ParentMachine].Add(p);
                }
                else
                {
                    saveStates[p.ParentMachine] = new HashSet<CPropertyBase>() { p };
                }
            }

            // Write structured data
            w.Write(saveStates.Count);
            foreach (CMachineState s in saveStates.Keys)
            {
                w.Write(s.Machine.Name);
                w.Write(s.Machine.DLL.Name);

                // Non-property smoothing
                s.WriteSmoothingInfo(w);

                // Properties and their values
                w.Write(saveStates[s].Count);
                foreach (CPropertyBase p in saveStates[s])
                {
                    p.WritePropertyInfo(w); // Info to retrieve the correct property
                    p.WritePropertyData(w); // Actual data
                }
            }

            CMachineStateData data = new CMachineStateData() { data = stream.ToArray() };

            return data;
        }

        // Legacy load code for file version <= 3
        private void RestoreLoadedData3()
        {
            if (_loadedState == null) return;

            MemoryStream stream = new MemoryStream(_loadedState.data);
            BinaryReader r = new BinaryReader(stream);

            // Options
            SelectNewMachines = r.ReadBoolean();
            CaptureOnSlotChange = r.ReadBoolean();
            RestoreOnSlotChange = r.ReadBoolean();
            RestoreOnSongLoad = r.ReadBoolean();
            RestoreOnStop = r.ReadBoolean();
            if (_loadedState.version >= 2) // New in file v2
            {
                SelectionFollowsSlot = r.ReadBoolean();
                ConfirmClear = r.ReadBoolean();
            }

            // Slot data (CMachineSnapshot.WriteData() x 128)
            for (int i = 0; i < 128; i++)
            {
                _slots[i].ReadData(r);
            }

            // MIDI map
            Int32 numMappings = r.ReadInt32();
            for (int n = 0; n < numMappings; n++)
            {
                string command = r.ReadString();
                int slot = r.ReadInt32();

                CMidiTargetInfo info = new CMidiTargetInfo(slot, command, this);
                CMidiEventSettings e = info.settings;
                e.ReadData(r);

                // Restore settings
                info.SetAction();
                if (MidiMap.Contains(info))
                {
                    // New system won't allow duplicates but old system might have.
                    // Discard if we hit one.
                    continue;
                }

                MidiMap.Add(info);

                var code = e.Encode();
                if (_midiMapping.ContainsKey(code))
                {
                    _midiMapping[code].Add(info.action); // mapping
                }
                else
                {
                    _midiMapping[code] = new HashSet<CMidiAction>() { info.action };
                }

                if (slot < 0) // Machine
                {
                    MidiInfo.Update(info);
                }
                else // Slot
                {
                    _slots[slot].MidiInfo.Update(info);
                }
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

                    if (_loadedState.version >= 3)
                    {
                        // Machine/Group smoothing - New in file version 3
                        s.SmoothingCount = r.ReadInt32(); // Machine
                        s.SmoothingUnits = r.ReadInt32(); // ^^
                        s.SmoothingShape = r.ReadInt32(); // ^^

                        s.GlobalStates.SmoothingCount = r.ReadInt32(); // Global group
                        s.GlobalStates.SmoothingUnits = r.ReadInt32(); // ^^
                        s.GlobalStates.SmoothingShape = r.ReadInt32(); // ^^

                        s.TrackStates.SmoothingCount = r.ReadInt32(); // Track group
                        s.TrackStates.SmoothingUnits = r.ReadInt32(); // ^^
                        s.TrackStates.SmoothingShape = r.ReadInt32(); // ^^
                        foreach (CPropertyBase pg in s.TrackStates.ChildProperties)
                        {
                            pg.SmoothingCount = r.ReadInt32(); // Track param group
                            pg.SmoothingUnits = r.ReadInt32(); // ^^
                            pg.SmoothingShape = r.ReadInt32(); // ^^
                        }
                    }

                    // Prior to file version 4 we assumed property names were unique.
                    // but this is not the case (see FSM Infector)
                    // To load saved data properly we need a dictionary of properties using name as the key
                    Dictionary<string, List<CPropertyBase>> properties = new Dictionary<string, List<CPropertyBase>>();
                    foreach (CPropertyBase p in s.AllProperties)
                    {
                        if (properties.ContainsKey(p.Name))
                        {
                            properties[p.Name].Add(p);
                        }
                        else
                        {
                            properties[p.Name] = new List<CPropertyBase>() { p };
                        }
                    }

                    Int32 count = r.ReadInt32(); // number of properties saved
                    for (Int32 i = 0; i < count; i++)
                    {
                        name = r.ReadString(); // Property name
                        int? track = r.ReadInt32(); // Property track (-1 if null)
                        if (track < 0) track = null;

                        CPropertyBase ps = properties[name].First(x => x.Track == track);

                        ps.Checked = r.ReadBoolean(); //Property selected

                        if (_loadedState.version >= 3)
                        {
                            ps.SmoothingCount = r.ReadInt32(); // New in file version 3
                            ps.SmoothingUnits = r.ReadInt32(); // ^^
                            ps.SmoothingShape = r.ReadInt32(); // ^^
                        }

                        Int32 numslots = r.ReadInt32(); // number of saved snapshot values
                        for (int j = 0; j < numslots; j++)
                        {
                            Int32 slot = r.ReadInt32(); // snapshot index
                            _slots[slot].ReadPropertyValue(ps, r); // Snapshot data (CMachineSnapshot.WriteProperty())
                        }

                        // Remove this property from the dictionary
                        properties[name].Remove(ps);
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                    return;
                }
            }

            PushSelection();
            PushSelectionM();

            CurrentSlot.OnPropertyChanged("HasData");
            SlotA.OnPropertyChanged("HasData");
            SlotB.OnPropertyChanged("HasData");

            OnPropertyChanged("CurrentSlot");
            OnPropertyChanged("State");
            OnPropertyChanged("Selection");
        }

        // Load code for file version 4 (and hopefully upwards)
        private void RestoreLoadedData()
        {
            if (_loadedState == null) return;

            if (_loadedState.version > CMachineStateData.currentVersion) throw new Exception("Version mismatch");

            MemoryStream stream = new MemoryStream(_loadedState.data);
            BinaryReader r = new BinaryReader(stream);

            // Options
            SelectNewMachines = r.ReadBoolean();
            CaptureOnSlotChange = r.ReadBoolean();
            RestoreOnSlotChange = r.ReadBoolean();
            RestoreOnSongLoad = r.ReadBoolean();
            RestoreOnStop = r.ReadBoolean();
            if (_loadedState.version >= 2) // New in file v2
            {
                SelectionFollowsSlot = r.ReadBoolean();
                ConfirmClear = r.ReadBoolean();
            }

            // Slot data (CMachineSnapshot.WriteData() x 128)
            for (int i = 0; i < 128; i++)
            {
                _slots[i].ReadData(r);
            }

            // MIDI map
            Int32 numMappings = r.ReadInt32();
            for (int n = 0; n < numMappings; n++)
            {
                string command = r.ReadString();
                int slot = r.ReadInt32();

                CMidiTargetInfo info = new CMidiTargetInfo(slot, command, this);
                CMidiEventSettings e = info.settings;
                e.ReadData(r);

                // Restore settings
                info.SetAction();
                if (MidiMap.Contains(info))
                    throw new Exception("MIDI mapping already exists"); // Shouldn't be possible

                MidiMap.Add(info);

                var code = e.Encode();
                if(_midiMapping.ContainsKey(code))
                {
                    _midiMapping[code].Add(info.action); // mapping
                }
                else
                {
                    _midiMapping[code] = new HashSet<CMidiAction>() { info.action };
                }

                if (slot < 0) // Machine
                {
                    MidiInfo.Update(info);
                }
                else // Slot
                {
                    _slots[slot].MidiInfo.Update(info);
                }
            }

            // Read data
            Int32 numStates = r.ReadInt32(); // number of saved states
            for (int n = 0; n < numStates; n++)
            {
                string name = r.ReadString(); // State name
                string dllname = r.ReadString(); // Machine DLL name
                try
                {
                    // Should be one and only one state matching both name and dllname. Exception if not.
                    CMachineState s = States.Single(x => x.Machine.Name == name && x.Machine.DLL.Name == dllname);

                    // Machine/Group smoothing
                    s.ReadSmoothingInfo(r);

                    // Properties and their values
                    Int32 count = r.ReadInt32(); // number of properties saved
                    CPropertyBase TEMPPREV = null;
                    for (Int32 i = 0; i < count; i++)
                    {
                        CPropertyBase p = s.FindPropertyFromSavedInfo(r); // Find the property
                        p.ReadPropertyData(r); // Restore data
                        TEMPPREV = p;
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                    return;
                }
            }

            PushSelection();
            PushSelectionM();

            CurrentSlot.OnPropertyChanged("HasData");
            SlotA.OnPropertyChanged("HasData");
            SlotB.OnPropertyChanged("HasData");

            OnPropertyChanged("CurrentSlot");
            OnPropertyChanged("State");
            OnPropertyChanged("Selection");
            OnPropertyChanged("MidiMap");
        }

        // Called after song load or template drop
        public void ImportFinished(IDictionary<string, string> machineNameMap)
        {
            if (_loadedState.version >= 4)
            {
                RestoreLoadedData();
            }
            else
            {
                RestoreLoadedData3(); // Legacy code for file version 3 and below
            }

            loading = false;

            if (RestoreOnSongLoad)
            {
                Restore();
            }
        }

        private void DoMidiAction(UInt32 code)
        {
            if (_midiMapping.ContainsKey(code))
            {
                foreach (CMidiAction item in _midiMapping[code])
                {
                    item.Trigger();
                }
            }
        }

        public void MidiNote(int channel, int note, int velocity)
        {
            const Byte noteOn = 1;
            const Byte noteOff = 2;

            // If MIDI dialog is open...
            if (MappingDialogSettings != null)
            {
                // If we're actually learning, store the values and stop learning
                if (MappingDialogSettings.Learning)
                {
                    MappingDialogSettings.Message = noteOn;
                    MappingDialogSettings.Channel = (Byte)channel;
                    MappingDialogSettings.Primary = (Byte)note;
                    MappingDialogSettings.Secondary = (Byte)128; // undefined
                    _mappingDialog.Learning = false;
                }
                return;
            }

            // This note on this channel
            UInt32 c1 = (UInt32)((noteOn << 24) | (128 /*v=any*/ << 16) | (note << 8) | channel);

            // This note on any channel
            UInt32 c2 = (UInt32)((noteOn << 24) | (128 /*v=any*/ << 16) | (note << 8) | 16 /*c=any*/);

            // Any note on this channel
            UInt32 c3 = (UInt32)((noteOn << 24) | (128 /*v=any*/ << 16) | (128 /*n=any*/ << 8) | channel);

            // Any note on any channel
            UInt32 c4 = (UInt32)((noteOn << 24) | (128 /*v=any*/ << 16) | (128 /*n=any*/ << 8) | 16 /*c=any*/);

            if (velocity == 0) //Note-off
            {
                c1 = c1 & 0xFFFFFF | (noteOff << 24);
                c2 = c2 & 0xFFFFFF | (noteOff << 24);
                c3 = c3 & 0xFFFFFF | (noteOff << 24);
                c4 = c4 & 0xFFFFFF | (noteOff << 24);
            }

            // Fire off matching actions, this note, this channel
            DoMidiAction(c1);

            // Fire off matching actions, this note, any channel
            DoMidiAction(c2);

            // Fire off matching actions, any note, this channel
            DoMidiAction(c3);

            // Fire off matching actions, any note, any channel
            DoMidiAction(c4);

            OnPropertyChanged("State");
        }

        public void MidiControlChange(int ctrl, int channel, int value)
        {
            const Byte msg = 3; // Controller

            // If MIDI dialog is open...
            if (MappingDialogSettings != null)
            {
                // If we're actually learning, store the values and stop learning
                if (MappingDialogSettings.Learning)
                {
                    MappingDialogSettings.Message = msg;
                    MappingDialogSettings.Channel = (Byte)channel;
                    MappingDialogSettings.Primary = (Byte)ctrl;
                    MappingDialogSettings.Secondary = (Byte)value;
                    _mappingDialog.Learning = false;
                    return;
                }
            }

            // This controller on this channel, with this value
            UInt32 c1 = (UInt32)((msg << 24) | (value << 16) | (ctrl << 8) | channel);

            // This controller on this channel, with any value
            UInt32 c2 = (UInt32)((msg << 24) | (128/*v=any*/ << 16) | (ctrl << 8) | channel);

            // This controller on any channel, with this value
            UInt32 c3 = (UInt32)((msg << 24) | (value << 16) | (ctrl << 8) | 16 /*c=any*/);

            // This controller on any channel, with any value
            UInt32 c4 = (UInt32)((msg << 24) | (128/*v=any*/ << 16) | (ctrl << 8) | 16 /*c=any*/);

            // Fire off matching actions, this controller, this channel, this value
            DoMidiAction(c1);

            // Fire off matching actions, this controller, this channel, any value
            DoMidiAction(c2);

            // Fire off matching actions, this controller, any channel, this value
            DoMidiAction(c3);

            // Fire off matching actions, this controller, any channel, any value
            DoMidiAction(c4);

            OnPropertyChanged("State");
        }

        #endregion IBuzzMachine

        #region events
        private void OnMachineAdded(IMachine m)
        {
            CMachineState ms;

            // Check if machine is being undeleted
            try
            {
                ms = States.Single(x => x.Machine.Name == m.Name);
                ms.Machine = m;
                ms.Active = true;
                VM.AddState(ms);
                OnPropertyChanged("State");
            }
            catch // Not found, new machine
            {
                if (m.ManagedMachine != this)
                {
                    ms = new CMachineState(this, m);
                    foreach (CPropertyBase s in ms.AllProperties)
                    {
                        s.Checked = SelectNewMachines;
                        AllProperties.Add(s);
                    }
                    States.Add(ms);
                    VM.AddState(ms);
                    OnPropertyChanged("State");
                }
                else
                {
                    ThisMachine = host.Machine;
                    SlotParam = ThisMachine.ParameterGroups[1].Parameters[0];
                }
            }
        }

        private void CheckChanged(object sender, TreeStateEventArgs e)
        {
            OnPropertyChanged("SelectionInfo");
        }

        private void OnMachineRemoved(IMachine m)
        {
            if (m != host.Machine)
            {
                CMachineState s = States[States.FindIndex(x => x.Machine == m)];
                s.Active = false;
                VM.RemoveState(s);
                OnPropertyChanged("State");
            }
            else
            {
                // This machine
                Interlocked.Exchange(ref workFlag, 1); // Signal to end the work thread

                Global.Buzz.Song.MachineAdded -= (x) => { OnMachineAdded(x); };
                Global.Buzz.Song.MachineRemoved -= (x) => { OnMachineRemoved(x); };

            }

        }
        #endregion events

        internal int _slotA;
        internal int _slotB;
        private CMappingDialog _mappingDialog;
        internal object changeLock;

        #region Global Parameters
        internal int _slot;
        [ParameterDecl(IsStateless = false, MinValue = 0, MaxValue = 127, DefValue = 0, Description = "Current slot shown in the main tab", Name = "Active Slot")]
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

        [ParameterDecl(IsStateless = false, MinValue = 0, MaxValue = Int16.MaxValue, DefValue = 0, Description = "Smoothing time when updating parameters", Name = "Smoothing")]
        public int SmoothingCount
        {
            get; set;
        }

        [ParameterDecl(IsStateless = false, MinValue = 0, MaxValue = 4, DefValue = 0, Description = "Time units for parameter smoothing", Name = "Smoothing units", ValueDescriptions = new String[] { "Ticks", "Beats", "Milliseconds", "Seconds", "Minutes" })]
        public int SmoothingUnits
        {
            get; set;
        }

        [ParameterDecl(IsStateless = false, MinValue = 0, MaxValue = 6, DefValue = 0, Description = "Shape for parameter smoothing", Name = "Smoothing shape", ValueDescriptions = new String[] { "Linear", "Cos", "I.Cos", "Quartic", "I.Quartic", "Cos S", "Cos Q" })]
        public int SmoothingShape
        {
            get; set;
        }

        public CMidiEventSettings MappingDialogSettings { get; internal set; }

        #endregion Global Parameters

        #region Commands

        internal void Capture()
        {
            CurrentSlot.Capture(Selection, false);
            OnPropertyChanged("State");
        }

        internal void CaptureMissing()
        {
            CurrentSlot.CaptureMissing(Selection);
            OnPropertyChanged("State");
        }

        internal void Restore(object param = null) // This one is for the worker thread
        {
            CMachineSnapshot s = CurrentSlot;
            if(s.HasData)
            {
                s.Restore();
            }
        }

        internal void Restore() // This one is for MIDI
        {
            CMachineSnapshot s = CurrentSlot;
            if (s.HasData)
            {
                s.Restore();
            }
        }

        internal void Purge()
        {
            string msg = string.Format("Discard {0} stored properties?", CurrentSlot.RedundantCount);
            if (Confirm("Confirm purge", msg))
            {
                CurrentSlot.Purge(Selection, false);
                OnPropertyChanged("State");
            }
        }

        internal void Clear()
        {
            string msg = string.Format("Discard all stored properties from {0}?", CurrentSlot.Name);
            if (Confirm("Confirm clear", msg))
            {
                CurrentSlot.Clear(false);
                OnPropertyChanged("State");
            }
        }

        internal void ClearAll()
        {
            string msg = string.Format("Discard all stored properties from all slots? Are you sure?");
            if (Confirm("Confirm clear all", msg, true))
            {
                foreach(CMachineSnapshot slot in Slots)
                {
                    slot.Clear(false);
                }
                OnPropertyChanged("State");
            }
        }

        internal void ClearSelected()
        {
            string msg = string.Format("Discard {0} stored properties from {1}?", Selection.Count, CurrentSlot.Name);
            if (Confirm("Confirm clear", msg))
            {
                CurrentSlot.Remove(Selection);
                OnPropertyChanged("State");
            }
        }

        internal void ClearSelectedAll()
        {
            string msg = string.Format("Discard {0} selected properties from all slots? Are you sure?", Selection.Count);
            if (Confirm("Confirm clear", msg, true))
            {
                HashSet<CPropertyBase> sel = Selection;
                foreach (CMachineSnapshot slot in Slots)
                {
                    slot.Remove(sel);
                }
                OnPropertyChanged("State");
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

        internal void ForceRestore()
        {
            if (!loading)
            {
                Restore();
                OnPropertyChanged("State");
                OnPropertyChanged("CurrentSlot");
            }
        }

        // Called after the slot has changed
        internal void OnSlotChanged()
        {
            if (_restoreOnSlotChange && !loading)
            {
                ThreadPool.QueueUserWorkItem(Restore);
            }

            if (_selectionFollowsSlot)
            {
                PushSelection();
            }

            OnPropertyChanged("Slot");
            OnPropertyChanged("State");
            OnPropertyChanged("CurrentSlot");
        }

        #endregion Commands

        public event PropertyChangedEventHandler PropertyChanged;
        public virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class Misc
    {
        // Byte count to formatted string solution - https://stackoverflow.com/a/48467634
        private static readonly string[] suffixes = new[] { "b", "K", "M", "G", "T", "P" };
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
