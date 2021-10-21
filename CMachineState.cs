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

namespace Snapshot
{
    public class StateChangedEventArgs : EventArgs
    {
        public IPropertyState Property { get; set; }
        public bool Selected { get; set; }
    }

    public interface INamed
    {
        string Name { get; }
        string DisplayName { get; }
    }

    public interface ISelectable : INamed
    {
        bool Selected { get; set; }
        event EventHandler<StateChangedEventArgs> SelChanged;
        void OnSelChanged(StateChangedEventArgs e);
    }

    public interface IPropertyState : ISelectable
    {
        int? Track { get; }
        int Size { get; }
        bool GotValue { get; }
        bool Active { get; set; }
        CMachine Owner { get; }
        CMachineState Parent { get; }

        event EventHandler SizeChanged;
        void OnSizeChanged();
    }

    public interface IGroup<T> : INamed
    {
        List<T> Children { get; }
    }

    public class CPropertyBase : IPropertyState
    {
        public CPropertyBase(CMachine owner, CMachineState parent)
        {
            _owner = owner;
            _parent = parent;
            _active = true;
            m_selected = false;
            Track = null;
        }

        private readonly CMachine _owner;
        public CMachine Owner => _owner;

        private readonly CMachineState _parent;
        public CMachineState Parent => _parent;

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

        protected bool m_selected;
        virtual public bool Selected
        {
            get => m_selected;
            set
            {
                if(m_selected != value)
                {
                    m_selected = value;
                    OnSelChanged(new StateChangedEventArgs() { Property = this, Selected = Selected });
                }
            }
        }

        virtual public string Name => throw new NotImplementedException();

        virtual public string DisplayName => throw new NotImplementedException();

        public event EventHandler<StateChangedEventArgs> SelChanged;
        public event EventHandler SizeChanged;

        public void OnSelChanged(StateChangedEventArgs e)
        {
            EventHandler<StateChangedEventArgs> handler = SelChanged;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        public void OnSizeChanged()
        {
            EventHandler handler = SizeChanged;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }
    }

    public class CParameterState : CPropertyBase
    {
        public CParameterState(CMachine owner, CMachineState parent, IParameter param, int? track = null)
            : base(owner, parent)
        {
            Parameter = param;
            Track = track;
        }

        public IParameter Parameter { get; private set; }

        public override string Name => Parameter.Name;

        public override string DisplayName => Track == null ? Parameter.Name : Track.ToString();

        public override int Size => sizeof(int);
    }

    public class CAttributeState : CPropertyBase
    {
        public CAttributeState(CMachine owner, CMachineState parent, IAttribute attr)
            : base(owner, parent)
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
        public CDataState(CMachine owner, CMachineState parent, IMachine machine)
            : base(owner, parent)
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
                    var stored = Owner.CurrentSlot.GetPropertySize(this);
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
                OnSizeChanged();
            }));
        }
    }

    public class CPropertyStateGroup : CPropertyBase, IGroup<IPropertyState>
    {
        public CPropertyStateGroup(CMachine owner, CMachineState parent, string name)
            : base(owner, parent)
        {
            Name = name;
            Children = new List<IPropertyState>();
        }

        public override string Name { get; }

        public override bool GotValue
        {
            get
            {
                try
                {
                    IPropertyState v = Children.First(x => x.GotValue);
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
               foreach(var c in Children)
                {
                    c.Active = value;
                }
            }
        }

        public List<IPropertyState> Children { get; }
    }

    public class CTrackPropertyStateGroup : CPropertyBase, IGroup<CPropertyStateGroup>
    {
        public CTrackPropertyStateGroup(CMachine owner, CMachineState parent, string name)
            : base(owner, parent)
        {
            Name = name;
            Children = new List<CPropertyStateGroup>();
        }

        public override string Name { get; }

        public override bool GotValue
        {
            get
            {
                try
                {
                    IPropertyState v = Children.First(x => x.GotValue);
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
                foreach (var c in Children)
                {
                    c.Active = value;
                }
            }
        }

        public List<CPropertyStateGroup> Children { get; }
    }

    public class CMachineState
    {
        public CMachineState(CMachine owner, IMachine m)
        {
            Machine = m;
            _owner = owner;
            _active = true;

            _allProperties = new List<IPropertyState>();

            if((Machine.DLL.Info.Flags & MachineInfoFlags.LOAD_DATA_RUNTIME) == MachineInfoFlags.LOAD_DATA_RUNTIME && Machine.Data != null)
            {
                DataState = new CDataState(owner, this, m);
                _allProperties.Add(DataState);
            }

            GlobalStates = new CPropertyStateGroup(owner, this, "Global");
            foreach (var p in Machine.ParameterGroups.Single(x => x.Type == ParameterGroupType.Global).Parameters)
            {
                if (p.Flags.HasFlag(ParameterFlags.State))
                {
                    var ps = new CParameterState(owner, this, p);
                    GlobalStates.Children.Add(ps);
                    _allProperties.Add(ps);
                }
            }

            TrackStates = new CTrackPropertyStateGroup(owner, this, "Track");
            var tracks = Machine.ParameterGroups.Single(x => x.Type == ParameterGroupType.Track);
            foreach (var p in tracks.Parameters)
            {
                if (p.Flags.HasFlag(ParameterFlags.State))
                {
                    var pg = new CPropertyStateGroup(owner, this, p.Name);
                    TrackStates.Children.Add(pg);
                    for(int i = 0; i < tracks.TrackCount; i++)
                    {
                        var ps = new CParameterState(owner, this, p, i);
                        pg.Children.Add(ps);
                        _allProperties.Add(ps);
                    }
                }
            }

            AttributeStates = new CPropertyStateGroup(owner, this, "Attributes");
            foreach (var a in Machine.Attributes)
            {
                var ats = new CAttributeState(owner, this, a);
                AttributeStates.Children.Add(ats);
                _allProperties.Add(ats);
            }
        }

        public IMachine Machine { get; internal set; }
        readonly CMachine _owner;

        public bool GotValue
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

        private bool _active;
        public bool Active
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

        // Storage. The publics are for the treeview
        // the private is to make capture etc. simpler
        public CDataState DataState { get; set; }
        public CPropertyStateGroup GlobalStates { get; private set; }
        public CTrackPropertyStateGroup TrackStates { get; private set; }
        public CPropertyStateGroup AttributeStates { get; private set; }
        private readonly List<IPropertyState> _allProperties;
        public List<IPropertyState> AllProperties => _allProperties;
    }
}
