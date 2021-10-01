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
        public ParameterState(IParameter param)
        {
            Parameter = param;
            Selected = false;
            Value = null;
        }

        public IParameter Parameter{ get; private set; }
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

    public class PropertyStateGroup
    {
        public PropertyStateGroup(string name)
        {
            Name = name;
            Properties = new List<IPropertyState>();
        }

        public string Name;
        public List<IPropertyState> Properties;
    }

    public class MachineState
    {
        public MachineState(IMachine m)
        {
            Machine = m;
            UseData = false;
            GotState = false;
            MissingCount = 0;
            RedundantCount = 0;

            _allStates = new List<IPropertyState>();

            InputStates = new PropertyStateGroup("Input");
            foreach(var p in Machine.ParameterGroups.Single(x => x.Type == ParameterGroupType.Input).Parameters)
            {
                if(p.Flags.HasFlag(ParameterFlags.State))
                {
                    var ps = new ParameterState(p);
                    ps.StateChanged += OnPropertyStateChanged;
                    InputStates.Properties.Add(ps);
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
                    GlobalStates.Properties.Add(ps);
                    _allStates.Add(ps);
                }
            }

            TrackStates = new PropertyStateGroup("Track");
            var tracks = Machine.ParameterGroups.Single(x => x.Type == ParameterGroupType.Track);
            foreach (var p in tracks.Parameters)
            {
                if (p.Flags.HasFlag(ParameterFlags.State))
                {
                    var ps = new ParameterState(p);
                    ps.StateChanged += OnPropertyStateChanged;
                    TrackStates.Properties.Add(ps);
                    _allStates.Add(ps);
                }
            }

            AttributeStates = new PropertyStateGroup("Attributes");
            foreach (var a in Machine.Attributes)
            {
                var ats = new AttributeState(a);
                ats.StateChanged += OnPropertyStateChanged;
                AttributeStates.Properties.Add(ats);
                _allStates.Add(ats);
            }
        }

        private void OnPropertyStateChanged(object sender, StateChangedEventArgs e)
        {
            if(e.Selected)
            {
                if(e.Property.GotValue)
                {
                    RedundantCount--;
                }
                else
                {
                    MissingCount++;
                }
            }
            else
            {
                if (e.Property.GotValue)
                {
                    RedundantCount++;
                }
                else
                {
                    MissingCount--;
                }
            }
        }

        public IMachine Machine { get; private set; }

        // True if anything is stored
        public bool GotState { get; private set; }

        // How many states are stored that aren't selected
        public int RedundantCount { get; private set; }

        // How many selected states have not been captured
        public int MissingCount { get; private set; }

        // Whether to include machine data
        public bool UseData { get; set; }

        // State storage. The publics are for the treeview
        // the private is to make capture etc. simpler
        public PropertyStateGroup InputStates { get; private set; }
        public PropertyStateGroup GlobalStates { get; private set; }
        public PropertyStateGroup TrackStates { get; private set; }
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
