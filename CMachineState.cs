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
    }

    public interface IGroup<T> : INamed
    {
        List<T> Children { get; }
    }

    public class CPropertyBase : IPropertyState
    {
        public CPropertyBase()
        {
            m_selected = false;
            Track = null;
        }

        virtual public int? Track { get; protected set; }

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

        public event EventHandler<StateChangedEventArgs> SelChanged;
        public void OnSelChanged(StateChangedEventArgs e)
        {
            EventHandler<StateChangedEventArgs> handler = SelChanged;
            if (handler != null)
            {
                handler(this, e);
            }
        }
    }

    public class CParameterState : CPropertyBase
    {
        public CParameterState(IParameter param, int? track = null)
        {
            Parameter = param;
            Track = track;
        }

        public IParameter Parameter { get; private set; }

        public override string Name => Track == null ? Parameter.Name : Track.ToString();

        public override int Size => sizeof(int);
    }

    public class CAttributeState : CPropertyBase
    {
        public CAttributeState(IAttribute attr)
        {
            Attribute = attr;
        }

        public IAttribute Attribute { get; private set; }

        public override string Name => Attribute.Name;

        public override int Size => sizeof(int);
    }

    public class CDataState : CPropertyBase
    {
        public CDataState(IMachine machine)
        {
            Machine = machine;
        }

        public IMachine Machine { get; private set; }

        public override string Name => "Data";

        public override int? Track => null;

        public override int Size => Machine.Data.Length;
    }

    public class CPropertyStateGroup : CPropertyBase, IGroup<IPropertyState>
    {
        public CPropertyStateGroup(string name)
        {
            Name = name;
            Children = new List<IPropertyState>();
        }

        public override string Name { get; }

        public List<IPropertyState> Children { get; }
    }

    public class CTrackPropertyStateGroup : CPropertyBase, IGroup<CPropertyStateGroup>
    {
        public CTrackPropertyStateGroup(string name)
        {
            Name = name;
            Children = new List<CPropertyStateGroup>();
        }

        public override string Name { get; }

        public List<CPropertyStateGroup> Children { get; }
    }

    public class CMachineState
    {
        public CMachineState(IMachine m)
        {
            Machine = m;

            _allProperties = new List<IPropertyState>();

            if((Machine.DLL.Info.Flags & MachineInfoFlags.LOAD_DATA_RUNTIME) == MachineInfoFlags.LOAD_DATA_RUNTIME && Machine.Data != null)
            {
                DataState = new CDataState(m);
                _allProperties.Add(DataState);
            }

            GlobalStates = new CPropertyStateGroup("Global");
            foreach (var p in Machine.ParameterGroups.Single(x => x.Type == ParameterGroupType.Global).Parameters)
            {
                if (p.Flags.HasFlag(ParameterFlags.State))
                {
                    var ps = new CParameterState(p);
                    GlobalStates.Children.Add(ps);
                    _allProperties.Add(ps);
                }
            }

            TrackStates = new CTrackPropertyStateGroup("Track");
            var tracks = Machine.ParameterGroups.Single(x => x.Type == ParameterGroupType.Track);
            foreach (var p in tracks.Parameters)
            {
                if (p.Flags.HasFlag(ParameterFlags.State))
                {
                    var pg = new CPropertyStateGroup(p.Name);
                    TrackStates.Children.Add(pg);
                    for(int i = 0; i < tracks.TrackCount; i++)
                    {
                        var ps = new CParameterState(p, i);
                        pg.Children.Add(ps);
                        _allProperties.Add(ps);
                    }
                }
            }

            AttributeStates = new CPropertyStateGroup("Attributes");
            foreach (var a in Machine.Attributes)
            {
                var ats = new CAttributeState(a);
                AttributeStates.Children.Add(ats);
                _allProperties.Add(ats);
            }
        }

        public IMachine Machine { get; private set; }

        // How many properties are selected
        public int SelCount => _allProperties.Count(x => x.Selected);

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
