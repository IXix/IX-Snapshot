using BuzzGUI.Interfaces;
using BuzzGUI.Common.Presets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Snapshot
{
    public class ParameterState
    {
        public ParameterState(IParameter param)
        {
            Parameter = param;
            Selected = false;
        }

        public IParameter Parameter{ get; private set; }
        public bool Selected { get; set; }
        public string Name { get { return Parameter.Name; } }
    }

    public class AttributeState
    {
        public AttributeState(IAttribute attr)
        {
            Attribute = attr;
            Selected = false;
        }

        public IAttribute Attribute { get; private set; }
        public bool Selected { get; set; }
        public string Name { get { return Attribute.Name; } }
    }

    public class ParameterStateGroup
    {
        public ParameterStateGroup(string name)
        {
            Name = name;
            Properties = new List<ParameterState>();
        }

        public string Name;
        public List<ParameterState> Properties;
    }

    public class AttributeStateGroup
    {
        public AttributeStateGroup(string name)
        {
            Name = name;
            Properties = new List<AttributeState>();
        }

        public string Name;
        public List<AttributeState> Properties;
    }

    public class MachineState
    {
        public MachineState(IMachine m)
        {
            Machine = m;
            UseData = false;
            GotState = false;

            InputStates = new ParameterStateGroup("Input");
            foreach(var p in Machine.ParameterGroups.Single(x => x.Type == ParameterGroupType.Input).Parameters)
            {
                InputStates.Properties.Add(new ParameterState(p));
            }

            GlobalStates = new ParameterStateGroup("Global");
            foreach (var p in Machine.ParameterGroups.Single(x => x.Type == ParameterGroupType.Global).Parameters)
            {
                GlobalStates.Properties.Add(new ParameterState(p));
            }

            TrackStates = new ParameterStateGroup("Track");
            foreach (var p in Machine.ParameterGroups.Single(x => x.Type == ParameterGroupType.Track).Parameters)
            {
                TrackStates.Properties.Add(new ParameterState(p));
            }

            AttributeStates = new AttributeStateGroup("Attributes");
            foreach (var a in Machine.Attributes)
            {
                AttributeStates.Properties.Add(new AttributeState(a));
            }
        }

        public IMachine Machine { get; private set; }

        // FIXME: True if anything is stored
        public bool GotState { get; private set; }

        // Whether to include machine data
        public bool UseData { get; set; }

        // State storage
        public ParameterStateGroup InputStates { get; private set; }
        public ParameterStateGroup GlobalStates { get; private set; }
        public ParameterStateGroup TrackStates { get; private set; }
        public AttributeStateGroup AttributeStates { get; private set; }

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
