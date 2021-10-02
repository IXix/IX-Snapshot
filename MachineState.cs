using BuzzGUI.Interfaces;
using BuzzGUI.Common.Presets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

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

    public interface IValueContainer
    {
        bool GotValue { get; }
        // FIXME: Capture/Restore etc.
    }

    public interface IPropertyState : ISelectable, IValueContainer
    {
        int? Track { get; }
    }

    public interface IGroup<T> : INamed
    {
        List<T> Children { get; }
    }

    public class ParameterState : IPropertyState
    {
        public ParameterState(IParameter param, int? track = null)
        {
            Parameter = param;
            Selected = false;
            Value = null;
            Track = track;
        }

        public IParameter Parameter { get; private set; }
        public int? Value { get; set; }

        public string Name => Track == null ?Parameter.Name : Track.ToString();

        public int? Track { get; private set; }
        public bool GotValue { get { return Value != null; } }

        public bool Selected { get; set; }
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

    public class AttributeState : IPropertyState
    {
        public AttributeState(IAttribute attr)
        {
            Attribute = attr;
            Selected = false;
        }

        public IAttribute Attribute { get; private set; }
        public int? Value { get; set; }

        public string Name => Attribute.Name;

        public int? Track { get; private set; }
        public bool GotValue => Value != null;

        public bool Selected { get; set; }
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

    public class DataState : IPropertyState
    {
        public byte [] Value { get; set; }

        public string Name => "Data";

        public int? Track => null;
        public bool GotValue => Value != null;

        public bool Selected { get; set; }
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

    public class PropertyStateGroup : IGroup<IPropertyState>
    {
        public PropertyStateGroup(string name)
        {
            Name = name;
            Children = new List<IPropertyState>();
        }

        public string Name { get; }

        public List<IPropertyState> Children { get; }
    }

    public class TrackPropertyStateGroup : IGroup<PropertyStateGroup>
    {
        public TrackPropertyStateGroup(string name)
        {
            Name = name;
            Children = new List<PropertyStateGroup>();
        }

        public string Name { get; }

        public List<PropertyStateGroup> Children { get; }
    }

    public class MachineState
    {
        public MachineState(IMachine m)
        {
            Machine = m;
            GotState = false;

            _allProperties = new List<IPropertyState>();

            DataStates = new DataState();
            _allProperties.Add(DataStates);

            InputStates = new PropertyStateGroup("Input");
            foreach(var p in Machine.ParameterGroups.Single(x => x.Type == ParameterGroupType.Input).Parameters)
            {
                if(p.Flags.HasFlag(ParameterFlags.State))
                {
                    var ps = new ParameterState(p);
                    ps.SelChanged += OnPropertyStateChanged;
                    InputStates.Children.Add(ps);
                    _allProperties.Add(ps);
                }
            }

            GlobalStates = new PropertyStateGroup("Global");
            foreach (var p in Machine.ParameterGroups.Single(x => x.Type == ParameterGroupType.Global).Parameters)
            {
                if (p.Flags.HasFlag(ParameterFlags.State))
                {
                    var ps = new ParameterState(p);
                    ps.SelChanged += OnPropertyStateChanged;
                    GlobalStates.Children.Add(ps);
                    _allProperties.Add(ps);
                }
            }

            TrackStates = new TrackPropertyStateGroup("Track");
            var tracks = Machine.ParameterGroups.Single(x => x.Type == ParameterGroupType.Track);
            foreach (var p in tracks.Parameters)
            {
                if (p.Flags.HasFlag(ParameterFlags.State))
                {
                    var pg = new PropertyStateGroup(p.Name);
                    TrackStates.Children.Add(pg);
                    for(int i = 0; i < tracks.TrackCount; i++)
                    {
                        var ps = new ParameterState(p, i);
                        ps.SelChanged += OnPropertyStateChanged;
                        pg.Children.Add(ps);
                        _allProperties.Add(ps);
                    }
                }
            }

            AttributeStates = new PropertyStateGroup("Attributes");
            foreach (var a in Machine.Attributes)
            {
                var ats = new AttributeState(a);
                ats.SelChanged += OnPropertyStateChanged;
                AttributeStates.Children.Add(ats);
                _allProperties.Add(ats);
            }
        }

        private void OnPropertyStateChanged(object sender, StateChangedEventArgs e)
        {
        }

        public IMachine Machine { get; private set; }

        // True if anything is stored
        public bool GotState { get; private set; }

        // How many selected states have not been captured
        public int SelCount
        {
            get { return _allProperties.Count(x => x.Selected == true); }
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

        // State storage. The publics are for the treeview
        // the private is to make capture etc. simpler
        public DataState DataStates { get; set; }
        public PropertyStateGroup InputStates { get; private set; }
        public PropertyStateGroup GlobalStates { get; private set; }
        public TrackPropertyStateGroup TrackStates { get; private set; }
        public PropertyStateGroup AttributeStates { get; private set; }
        private readonly List<IPropertyState> _allProperties;
        public List<IPropertyState> AllProperties { get { return _allProperties; } }

        public bool Capture()
        {
            return GotState;
        }

        public bool Restore()
        {
            return true;
        }

        public void Clear()
        {
            GotState = false;
        }

        public void Purge()
        {
            // FIXME: Remove stored state for unselected items
        }
    }
}
