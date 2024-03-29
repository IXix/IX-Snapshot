﻿using BuzzGUI.Interfaces;
using BuzzGUI.Common.Presets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;
using System.ComponentModel;

namespace Snapshot
{
    public class TreeStateEventArgs : EventArgs
    {
        public IPropertyState Property { get; set; }
        public bool? Checked { get; set; }
        public bool? Checked_M { get; set; }
        public bool Expanded { get; set; }
        public bool Expanded_M { get; set; }
        public bool Selected { get; set; }
    }

    public class PropertyStateEventArgs : EventArgs
    {
        public IPropertyState Property { get; set; }
        public bool GotValue { get; set; }
        public int Size { get; set; }
        public bool HasSmoothing { get; set; }
        public bool ChildHasSmoothing { get; set; }
    }

    public interface INamed
    {
        string Name { get; }
        string DisplayName { get; }
    }

    public interface ITreeNode : INamed
    {
        bool? Checked { get; set; }
        bool? Checked_M { get; set; }
        bool Expanded { get; set; }
        bool Expanded_M { get; set; }

        event EventHandler<TreeStateEventArgs> TreeStateChanged;
        void OnTreeStateChanged();
    }

    public interface IPropertyState : ITreeNode, ISmoothable
    {
        int? Track { get; }
        int Size { get; }
        bool GotValue { get; }
        bool Active { get; set; }
        CMachine Owner { get; }
        CMachineState ParentMachine { get; }

        event EventHandler PropertyStateChanged;
        void OnPropertyStateChanged();
    }

    public interface ISmoothable
    {
        int? SmoothingCount { get; set; }
        int? SmoothingUnits { get; set; }
        bool AllowSmoothing { get; set; }
    }

    public class CPropertyBase : IPropertyState
    {
        public CPropertyBase(CMachine owner, CPropertyBase parent, CMachineState parentMachine)
        {
            _owner = owner;
            _parent = parent;
            _parentMachine = parentMachine;
            _active = true;

            m_maxDigits = 0;

            m_checked = false;
            m_checked_M = false;
            m_expanded = false;
            m_expanded_M = false;

            m_smoothingCount = null;
            m_smoothingUnits = null;
            m_smoothingAllow = true;
            m_editingAllow = true;
            Track = null;
            ChildProperties = new HashSet<CPropertyBase>();
        }

        private readonly CMachine _owner;
        public CMachine Owner => _owner;

        private readonly CMachineState _parentMachine;
        public CMachineState ParentMachine => _parentMachine;

        private readonly CPropertyBase _parent;
        public CPropertyBase Parent => _parent;

        public HashSet<CPropertyBase> ChildProperties { get; private set; }

        virtual public int? Track { get; protected set; }

        virtual public int? CurrentValue
        {
            get => null;
            set { return; } // Do nothing
        }

        virtual public bool GotValue => _owner.CurrentSlot.ContainsProperty(this);

        protected bool _active;
        virtual public bool Active
        {
            get => _active;
            set
            {
                _active = value;
            }
        }

        virtual public int Size => 0;

        public bool HasSmoothing
        {
            get
            {
                if (!m_smoothingAllow) return false;

                return m_smoothingCount != null || m_smoothingUnits != null || m_smoothingShape != null;
            }
        }

        public bool ChildHasSmoothing
        {
            get
            {
                foreach (var p in ChildProperties)
                {
                    if (p.HasSmoothing)
                    {
                        return true;
                    }
                }
                return false;
            }
        }


        protected bool? m_checked;
        virtual public bool? Checked
        {
            get => m_checked;
            set
            {
                if(m_checked != value)
                {
                    m_checked = value;
                    OnTreeStateChanged();
                }
            }
        }

        protected bool? m_checked_M;
        virtual public bool? Checked_M
        {
            get => m_checked_M;
            set
            {
                if (m_checked_M != value)
                {
                    m_checked_M = value;
                    OnTreeStateChanged();
                }
            }
        }

        private bool m_expanded;
        public bool Expanded
        {
            get => m_expanded;
            set
            {
                if (m_expanded != value)
                {
                    m_expanded = value;
                    OnTreeStateChanged();
                }
            }
        }

        private bool m_expanded_M;
        public bool Expanded_M
        {
            get => m_expanded_M;
            set
            {
                if (m_expanded_M != value)
                {
                    m_expanded_M = value;
                    OnTreeStateChanged();
                }
            }
        }

        virtual public string Name => throw new NotImplementedException();

        virtual public string DisplayName => Name;

        virtual public string DisplayValue => "";

        protected int? m_smoothingCount;
        virtual public int? SmoothingCount
        {
            get => m_smoothingCount;
            set
            {
                if (value < 0)
                    value = null;
                m_smoothingCount = value;

                OnPropertyStateChanged();
            }
        }

        protected int? m_smoothingUnits;
        virtual public int? SmoothingUnits
        {
            get => m_smoothingUnits;
            set
            {
                if (value < 0)
                    value = null;
                m_smoothingUnits = value;

                OnPropertyStateChanged();
            }
        }

        protected int? m_smoothingShape;
        virtual public int? SmoothingShape
        {
            get => m_smoothingShape;
            set
            {
                if (value < 0)
                    value = null;
                m_smoothingShape = value;

                OnPropertyStateChanged();
            }
        }

        protected bool m_smoothingAllow;
        public bool AllowSmoothing
        {
            get => m_smoothingAllow;
            set => m_smoothingAllow = value;
        }

        protected bool m_editingAllow;
        public bool AllowEditing
        {
            get => m_editingAllow;
            set => m_editingAllow = value;
        }

        virtual public string CurrentValueString => throw new NotImplementedException();

        protected int m_maxDigits;
        public int MaxDigits => m_maxDigits;

        virtual public string GetValueDescription(int value)
        {
            return value.ToString();
        }

        public event EventHandler<TreeStateEventArgs> TreeStateChanged;
        public void OnTreeStateChanged()
        {
            TreeStateEventArgs args = new TreeStateEventArgs
            {
                Property = this,
                Checked = Checked,
                Checked_M = Checked_M,
                Expanded = Expanded,
                Expanded_M = Expanded_M
            };
            TreeStateChanged?.Invoke(this, args);
        }

        public event EventHandler PropertyStateChanged;
        public void OnPropertyStateChanged()
        {
            PropertyStateEventArgs args = new PropertyStateEventArgs
            {
                Property = this,
                GotValue = GotValue,
                Size = Size,
                HasSmoothing = HasSmoothing,
                ChildHasSmoothing = ChildHasSmoothing
            };
            PropertyStateChanged?.Invoke(this, args);
        }

        public virtual void WriteSmoothingInfo(BinaryWriter w)
        {
            w.Write((Int32)(SmoothingCount ?? -1));
            w.Write((Int32)(SmoothingUnits ?? -1));
            w.Write((Int32)(SmoothingShape ?? -1));
        }

        public virtual void ReadSmoothingInfo(BinaryReader r)
        {
            SmoothingCount = r.ReadInt32();
            SmoothingUnits = r.ReadInt32();
            SmoothingShape = r.ReadInt32();
        }

        public virtual void WritePropertyInfo(BinaryWriter w)
        {
            throw new Exception("Unexpected call to WritePropertyInfo!");
        }

        public void WritePropertyData(BinaryWriter w)
        {
            w.Write(Owner.m_selection.SelectedProperties.Contains(this)); // Can't use Selection, it may point to CurrentSlot.Selection
            w.Write(Owner.m_selectionM.SelectedProperties.Contains(this));// SelectionM would be okay but just in case..
            WriteSmoothingInfo(w);

            // Write slot specific data
            List<CMachineSnapshot> slots = Owner.Slots.Where(x => x.ContainsProperty(this) || x.Selection.Contains(this)).ToList();
            w.Write((Int32)slots.Count());
            foreach (CMachineSnapshot slot in slots)
            {
                w.Write((Int32)slot.Index);
                slot.WritePropertyValue(this, w);
                w.Write(slot.Selection.Contains(this)); // slot Selection is just the slot's own hashset
            }
        }

        public void ReadPropertyData(BinaryReader r)
        {
            // Read data
            bool selMain = r.ReadBoolean();
            if(selMain)
            {
                Owner.m_selection.SelectedProperties.Add(this);
            }

            Checked_M = r.ReadBoolean();

            ReadSmoothingInfo(r);

            // Read stored values
            Int32 n = r.ReadInt32(); // number of slots containing the property
            for (Int32 i = 0; i < n; i++)
            {
                Int32 idx = r.ReadInt32();
                CMachineSnapshot slot = Owner.Slots[idx];
                slot.ReadPropertyValue(this, r);
                bool selSlot = r.ReadBoolean();
                if (selSlot)
                {
                    slot.Selection.Add(this);
                }
            }

            OnPropertyStateChanged();
        }
    }

    public class CParameterState : CPropertyBase
    {
        public CParameterState(CMachine owner, CPropertyBase parent, CMachineState parentMachine, IParameter param, int? track = null)
            : base(owner, parent, parentMachine)
        {
            Machine = parentMachine.Machine;
            Parameter = param;
            Track = track;

            m_maxDigits = param.MaxValue.ToString().Length;
        }

        //public CPropertyBase Parent;

        public IMachine Machine { get; private set; }

        public IParameter Parameter { get; private set; }

        public override string Name => Parameter.Name;

        public override string DisplayName => Track == null ? Parameter.Name : Track.ToString();

        public override int Size => sizeof(int);

        public override int? CurrentValue => Parameter.GetValue(Track ?? 0);

        public override string CurrentValueString => GetValueDescription(Parameter.GetValue(Track ?? 0));

        public override string GetValueDescription(int value)
        {
            return Parameter.DescribeValue(value);
        }

        public override void WritePropertyInfo(BinaryWriter w)
        {
            w.Write((Byte)1); // Type 1 == param
            w.Write((Byte) Parameter.Group.Type);
            w.Write((Int32)Parameter.IndexInGroup);
            if(Parameter.Group.Type == ParameterGroupType.Track)
            {
                w.Write((Int32)Track);
            }
        }
    }

    public class CAttributeState : CPropertyBase
    {
        public CAttributeState(CMachine owner, CPropertyBase parent, CMachineState parentMachine, IAttribute attr)
            : base(owner, parent, parentMachine)
        {
            Attribute = attr;
            AllowSmoothing = false;

            m_maxDigits = attr.MaxValue.ToString().Length;
        }

        public IAttribute Attribute { get; private set; }

        public override string Name => Attribute.Name;

        public override string DisplayName => Attribute.Name;

        public override int Size => sizeof(int);

        public override int? CurrentValue => Attribute.Value;

        public override void WriteSmoothingInfo(BinaryWriter w)
        {
            // Can't be smoothed
        }

        public override void ReadSmoothingInfo(BinaryReader r)
        {
            // Can't be smoothed
        }

        public override void WritePropertyInfo(BinaryWriter w)
        {
            w.Write((Byte)0); // Type 0 == Attribute
            w.Write(Attribute.Name);
        }
    }

    public class CDataState : CPropertyBase
    {
        public CDataState(CMachine owner, CPropertyBase parent, CMachineState parentMachine, IMachine machine)
            : base(owner, parent, parentMachine)
        {
            Machine = machine;
            _size = 0;
            UpdateSize();
            AllowSmoothing = false;
            AllowEditing = false;
        }

        public IMachine Machine { get; private set; }

        public override string Name => "Data";

        public override string DisplayName
        {
            get
            {
                if(GotValue)
                {
                    int stored = Owner.CurrentSlot.GetPropertySize(this);
                    return string.Format("Data - {0} ({1})", Misc.ToSize(stored), Misc.ToSize(Size));
                }
                else
                {
                    return string.Format("Data - ({0})", Misc.ToSize(Size));
                }
            }
        }

        public override int? Track => null;

        private int _size;
        public override int Size => _size;

        internal void UpdateSize()
        {
            Application.Current.Dispatcher.BeginInvoke((Action)(() =>
            {
                try
                {
                    _size = Machine.Data.Length;
                    OnPropertyStateChanged();
                }
                catch(AccessViolationException) // Had one of these when closing Buzz while GUI was open.
                {
                    return;
                }
            }));
        }

        public override void WriteSmoothingInfo(BinaryWriter w)
        {
            // Can't be smoothed
        }

        public override void ReadSmoothingInfo(BinaryReader r)
        {
            // Can't be smoothed
        }

        public override void WritePropertyInfo(BinaryWriter w)
        {
            w.Write((Byte)2); // Type 2 == Data
        }
    }

    public class CPropertyStateGroup : CPropertyBase
    {
        public CPropertyStateGroup(CMachine owner, CPropertyBase parent, CMachineState parentMachine, string name)
            : base(owner, parent, parentMachine)
        {
            Name = name;
            AllowEditing = false;
        }

        public override string Name { get; }

        public override bool GotValue
        {
            get
            {
                try
                {
                    IPropertyState v = ChildProperties.First(x => x.GotValue);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        public override bool Active
        {
            get => _active;
            set
            {
                _active = value;
               foreach(IPropertyState c in ChildProperties)
                {
                    c.Active = value;
                }
            }
        }
    }

    public class CTrackPropertyStateGroup : CPropertyBase
    {
        public CTrackPropertyStateGroup(CMachine owner, CPropertyBase parent, CMachineState parentMachine, string name)
            : base(owner, parent, parentMachine)
        {
            Name = name;
            AllowEditing = false;
        }

        public override string Name { get; }

        public override bool GotValue
        {
            get
            {
                try
                {
                    _ = ChildProperties.First(x => x.GotValue);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        public override bool Active
        {
            get => _active;
            set
            {
                _active = value;
                foreach (CPropertyStateGroup c in ChildProperties)
                {
                    c.Active = value;
                }
            }
        }
    }

    public class CMachineState : CPropertyBase
    {
        public CMachineState(CMachine owner, IMachine m) :
            base(owner, null, null)
        {
            Machine = m;
            _trackCount = m.TrackCount;
            _owner = owner;
            _active = true;

            AllowEditing = false;

            _allProperties = new HashSet<CPropertyBase>();

            if((Machine.DLL.Info.Flags & MachineInfoFlags.LOAD_DATA_RUNTIME) == MachineInfoFlags.LOAD_DATA_RUNTIME && Machine.Data != null)
            {
                DataState = new CDataState(owner, this, this, m);
                _allProperties.Add(DataState);
                ChildProperties.Add(DataState);
            }

            GlobalStates = new CPropertyStateGroup(owner, this, this, "Global");
            ChildProperties.Add(GlobalStates);
            foreach (IParameter p in Machine.ParameterGroups.Single(x => x.Type == ParameterGroupType.Global).Parameters)
            {
                if (p.Flags.HasFlag(ParameterFlags.State))
                {
                    CParameterState ps = new CParameterState(owner, GlobalStates, this, p);
                    GlobalStates.ChildProperties.Add(ps);
                    _allProperties.Add(ps);
                }
            }

            TrackStates = new CTrackPropertyStateGroup(owner, this, this, "Track");
            ChildProperties.Add(TrackStates);
            IParameterGroup tracks = Machine.ParameterGroups.Single(x => x.Type == ParameterGroupType.Track);
            foreach (IParameter p in tracks.Parameters)
            {
                if (p.Flags.HasFlag(ParameterFlags.State))
                {
                    CPropertyStateGroup pg = new CPropertyStateGroup(owner, TrackStates, this, p.Name);
                    TrackStates.ChildProperties.Add(pg);
                    for(int i = 0; i < tracks.TrackCount; i++)
                    {
                        CParameterState ps = new CParameterState(owner, pg, this, p, i);
                        pg.ChildProperties.Add(ps);
                        _allProperties.Add(ps);
                    }
                }
            }

            AttributeStates = new CPropertyStateGroup(owner, this, this, "Attributes") { AllowSmoothing = false };
            ChildProperties.Add(AttributeStates);
            foreach (IAttribute a in Machine.Attributes)
            {
                CAttributeState ats = new CAttributeState(owner, AttributeStates, this, a);
                AttributeStates.ChildProperties.Add(ats);
                _allProperties.Add(ats);
            }

            m.PropertyChanged += OnMachinePropertyChanged;
        }

        private void OnMachinePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            IMachine m = sender as IMachine;

            if (m != Machine) return;

            switch (e.PropertyName)
            {

                case "TrackCount":
                    UpdateTracks();
                    break;

                default:
                    // "Attributes":
                    // "IsBypassed":
                    // "IsMuted":
                    // "IsSoloed":
                    // "IsActive":
                    // "IsWireless":
                    // "LastEngineThread" :
                    // "MIDIInputChannel":
                    // "Name":
                    // "OversampleFactor":
                    // "OverrideLatency":
                    // "Patterns":
                    // "PatternEditorDLL":
                    // "Position":
                    break;
            }
        }

        public IMachine Machine { get; internal set; }
        readonly CMachine _owner;

        public override string Name => Machine.Name;

        private int _trackCount;
        private int _highestTrackCount;

        public override bool GotValue
        {
            get
            {
                try
                {
                    IPropertyState v = AllProperties.First(x => x.GotValue);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        public override bool Active
        {
            get => _active;
            set
            {
                _active = value;
                foreach(IPropertyState p in AllProperties)
                {
                    p.Active = value;
                }
            }
        }

        private void UpdateTracks()
        {
            int newCount = Machine.TrackCount;
            int delta = Machine.TrackCount - _trackCount;
            if (delta < 0) // Track removed
            {
                foreach(CPropertyStateGroup g in TrackStates.ChildProperties) // for each track param
                {
                    foreach(IPropertyState p in g.ChildProperties.Where(x => x.Track > newCount - 1))
                    {
                        p.Active = false;
                    }
                }
            }
            else if(delta > 0) // Track added
            {
                if(newCount <= _highestTrackCount) // Previously added track restored
                {
                    
                    foreach (CPropertyStateGroup g in TrackStates.ChildProperties) // for each track param9
                    {
                        foreach (IPropertyState p in g.ChildProperties.Where(x => x.Track > _trackCount - 1 && x.Track < newCount))
                        {
                            p.Active = true;
                        }
                    }
                }
                else // New track
                {
                    while(delta > 0) // Not sure if this is necessary. Can multiple tracks be added at once?
                    {
                        int newIndex = newCount - delta;
                        foreach (CPropertyStateGroup g in TrackStates.ChildProperties) // for each track param
                        {
                            int gIndex = (g.ChildProperties.First() as CParameterState).Parameter.IndexInGroup;
                            IParameterGroup tracks = Machine.ParameterGroups.Single(x => x.Type == ParameterGroupType.Track);

                            CParameterState ps = new CParameterState(_owner, g, this, tracks.Parameters[gIndex], newIndex);
                            g.ChildProperties.Add(ps);
                            _allProperties.Add(ps);
                            _owner.AllProperties.Add(ps);
                        }
                        delta--;
                    }
                }
            }

            // Update treeview and info
            _owner.OnPropertyChanged("SelectionInfo");

            // Update tracking
            _trackCount = Machine.TrackCount;
            if(_trackCount > _highestTrackCount)
            {
                _highestTrackCount = _trackCount;
            }
        }

        public CPropertyBase FindPropertyFromSavedInfo(BinaryReader r)
        {
            // Read type specific info and find the correct property
            CPropertyBase p = null;
            Byte type = r.ReadByte();
            switch (type)
            {
                case 0: // Attribute
                    string name = r.ReadString();
                    p = AttributeStates.ChildProperties.Single(x => x.Name == name);
                    break;

                case 1: // Parameter
                    Byte group = r.ReadByte();
                    Int32 idx = r.ReadInt32();
                    Int32? track = null;
                    if (group == (Byte)ParameterGroupType.Track)
                    {
                        track = r.ReadInt32();
                    }

                    IParameter param = Machine.ParameterGroups[group].Parameters[idx];
                    p = AllProperties.Single(x => x is CParameterState && (x as CParameterState).Parameter == param && x.Track == track);
                    break;

                case 2: // Data
                    p = DataState;
                    break;

                default:
                    throw new Exception("Unexpected type in CMachineState.FindPropertyFromSavedInfo()");
            }

            return p;
        }

        public override void WriteSmoothingInfo(BinaryWriter w)
        {
            base.WriteSmoothingInfo(w); // Machine
            GlobalStates.WriteSmoothingInfo(w); // Global group
            TrackStates.WriteSmoothingInfo(w); // Track group
            foreach (CPropertyBase pg in TrackStates.ChildProperties)
            {
                pg.WriteSmoothingInfo(w); // Track param group
            }
        }

        public override void ReadSmoothingInfo(BinaryReader r)
        {
            base.ReadSmoothingInfo(r);
            GlobalStates.ReadSmoothingInfo(r); // Global group
            TrackStates.ReadSmoothingInfo(r); // Track group
            foreach (CPropertyBase pg in TrackStates.ChildProperties)
            {
                pg.ReadSmoothingInfo(r); // Track param group
            }
        }

        // Storage. The publics are for the treeview
        // the private is to make capture etc. simpler
        public CDataState DataState { get; set; }
        public CPropertyStateGroup GlobalStates { get; private set; }
        public CTrackPropertyStateGroup TrackStates { get; private set; }
        public CPropertyStateGroup AttributeStates { get; private set; }

        private readonly HashSet<CPropertyBase> _allProperties;
        public HashSet<CPropertyBase> AllProperties => _allProperties;
    }
}
