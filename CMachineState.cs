using BuzzGUI.Interfaces;
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
    }

    public class CPropertyBase : IPropertyState
    {
        public CPropertyBase(CMachine owner, CPropertyBase parent, CMachineState parentMachine)
        {
            _owner = owner;
            _parent = parent;
            _parentMachine = parentMachine;
            _active = true;

            m_checked = false;
            m_checked_M = false;
            m_expanded = false;
            m_expanded_M = false;

            m_smoothingCount = null;
            m_smoothingUnits = null;
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

        virtual public string DisplayValue => throw new NotImplementedException();

        protected int? m_smoothingCount;
        virtual public int? SmoothingCount
        {
            get => m_smoothingCount;
            set => m_smoothingCount = value;
        }

        protected int? m_smoothingUnits;
        virtual public int? SmoothingUnits
        {
            get => m_smoothingUnits;
            set => m_smoothingUnits = value;
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
                Size = Size
            };
            PropertyStateChanged?.Invoke(this, args);
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
        }

        //public CPropertyBase Parent;

        public IMachine Machine { get; private set; }

        public IParameter Parameter { get; private set; }

        public override string Name => Parameter.Name;

        public override string DisplayName => Track == null ? Parameter.Name : Track.ToString();

        public override int Size => sizeof(int);
    }

    public class CAttributeState : CPropertyBase
    {
        public CAttributeState(CMachine owner, CPropertyBase parent, CMachineState parentMachine, IAttribute attr)
            : base(owner, parent, parentMachine)
        {
            Attribute = attr;
        }

        public IAttribute Attribute { get; private set; }

        public override string Name => Attribute.Name;

        public override string DisplayName => Attribute.Name;

        public override int Size => sizeof(int);
    }

    public class CDataState : CPropertyBase
    {
        public CDataState(CMachine owner, CPropertyBase parent, CMachineState parentMachine, IMachine machine)
            : base(owner, parent, parentMachine)
        {
            Machine = machine;
            _size = 0;
            UpdateSize();
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
                _size = Machine.Data.Length;
                OnPropertyStateChanged();
            }));
        }
    }

    public class CPropertyStateGroup : CPropertyBase
    {
        public CPropertyStateGroup(CMachine owner, CPropertyBase parent, CMachineState parentMachine, string name)
            : base(owner, parent, parentMachine)
        {
            Name = name;
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

            _allProperties = new HashSet<CPropertyBase>();

            if((Machine.DLL.Info.Flags & MachineInfoFlags.LOAD_DATA_RUNTIME) == MachineInfoFlags.LOAD_DATA_RUNTIME && Machine.Data != null)
            {
                DataState = new CDataState(owner, this, this, m);
                _allProperties.Add(DataState);
            }

            GlobalStates = new CPropertyStateGroup(owner, this, this, "Global");
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

            AttributeStates = new CPropertyStateGroup(owner, this, this, "Attributes");
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
                case "Name":
                    //VM.OnPropertyChanged("Name"); //FIXME
                    break;

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
                        }
                        delta--;
                    }
                }
            }

            // Update treeview and info
            //VM.OnTrackCountChanged(newCount, _trackCount); // FIXME
            _owner.OnPropertyChanged("SelectionInfo");

            // Update tracking
            _trackCount = Machine.TrackCount;
            if(_trackCount > _highestTrackCount)
            {
                _highestTrackCount = _trackCount;
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
