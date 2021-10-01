using BuzzGUI.Interfaces;
using BuzzGUI.Common.Presets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Snapshot
{
    public interface IPropertyState
    {
        string Name { get; }
        int? Track { get; }
        bool Selected { get; set; }
        int? Value { get; set; }
        bool GotValue { get; }

        event EventHandler<StateChangedEventArgs> StateChanged;
        void OnStateChanged(StateChangedEventArgs e);
    }

    public class StateChangedEventArgs : EventArgs
    {
        public IPropertyState Property { get; set; }
        public bool Selected { get; set; }
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

        public IParameter Parameter{ get; private set; }
        public int? Track { get; private set; }
        public bool Selected { get; set; }
        public string Name { get { return Parameter.Name; } }

        // Stored value. null if not captured
        public int? Value { get; set; }

        public bool GotValue { get { return Value != null; } }


        public event EventHandler<StateChangedEventArgs> StateChanged;

        public void OnStateChanged(StateChangedEventArgs e)
        {
            EventHandler<StateChangedEventArgs> handler = StateChanged;
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
        public int? Track { get; private set; }
        public bool Selected { get; set; }
        public string Name { get { return Attribute.Name; } }

        // Stored value. null if not captured
        public int? Value { get; set; }

        public bool GotValue { get { return Value != null; } }

        public event EventHandler<StateChangedEventArgs> StateChanged;

        public void OnStateChanged(StateChangedEventArgs e)
        {
            EventHandler<StateChangedEventArgs> handler = StateChanged;
            if (handler != null)
            {
                handler(this, e);
            }
        }
    }

    public interface IGroup<T>
    {
        string Name { get; }
        List<T> Children { get; }
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
            UseData = false;
            GotState = false;

            _allStates = new List<IPropertyState>();

            InputStates = new PropertyStateGroup("Input");
            foreach(var p in Machine.ParameterGroups.Single(x => x.Type == ParameterGroupType.Input).Parameters)
            {
                if(p.Flags.HasFlag(ParameterFlags.State))
                {
                    var ps = new ParameterState(p);
                    ps.StateChanged += OnPropertyStateChanged;
                    InputStates.Children.Add(ps);
                    _allStates.Add(ps);
                }
            }

            GlobalStates = new PropertyStateGroup("Global");
            foreach (var p in Machine.ParameterGroups.Single(x => x.Type == ParameterGroupType.Global).Parameters)
            {
                if (p.Flags.HasFlag(ParameterFlags.State))
                {
                    var ps = new ParameterState(p);
                    ps.StateChanged += OnPropertyStateChanged;
                    GlobalStates.Children.Add(ps);
                    _allStates.Add(ps);
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
                        ps.StateChanged += OnPropertyStateChanged;
                        pg.Children.Add(ps);
                        _allStates.Add(ps);
                    }
                }
            }

            AttributeStates = new PropertyStateGroup("Attributes");
            foreach (var a in Machine.Attributes)
            {
                var ats = new AttributeState(a);
                ats.StateChanged += OnPropertyStateChanged;
                AttributeStates.Children.Add(ats);
                _allStates.Add(ats);
            }
        }

        private void OnPropertyStateChanged(object sender, StateChangedEventArgs e)
        {
            // FIXME: Trigger update of stats text
        }

        public IMachine Machine { get; private set; }

        // True if anything is stored
        public bool GotState { get; private set; }

        // How many selected states have not been captured
        public int SelCount
        {
            get { return _allStates.Count(x => x.Selected == true); }
        }

        // How many states are stored that aren't selected
        public int RedundantCount
        {
            get { return _allStates.Count(x => x.Selected == false && x.GotValue == true); }
        }

        // How many selected states have not been captured
        public int MissingCount
        {
            get { return _allStates.Count(x => x.Selected == true && x.GotValue == false); }
        }

        public string InfoSelCount
        {
            get { return string.Format("{0} of {1} properties selected", SelCount, _allStates.Count); }
        }

        // Whether to include machine data
        public bool UseData { get; set; }

        // State storage. The publics are for the treeview
        // the private is to make capture etc. simpler
        public PropertyStateGroup InputStates { get; private set; }
        public PropertyStateGroup GlobalStates { get; private set; }
        public TrackPropertyStateGroup TrackStates { get; private set; }
        public PropertyStateGroup AttributeStates { get; private set; }
        private List<IPropertyState> _allStates;

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
