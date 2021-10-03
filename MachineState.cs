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

        bool Capture();
        bool CaptureMissing();
        bool Restore();
        void Clear();
        void Purge();

        event EventHandler<StateChangedEventArgs> ValChanged;
        void OnValChanged(StateChangedEventArgs e);
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
            Track = track;
            Value = new int?();
        }

        public IParameter Parameter { get; private set; }
        public int? Value { get; set; }

        public string Name => Track == null ?Parameter.Name : Track.ToString();

        public int? Track { get; private set; }
        public bool GotValue => Value.HasValue;

        public bool Selected { get; set; }
        public event EventHandler<StateChangedEventArgs> SelChanged;
        public event EventHandler<StateChangedEventArgs> ValChanged;

        public bool Capture()
        {
            if(Selected)
            {
                Value = Parameter.GetValue(Track ?? 0);
                OnValChanged(new StateChangedEventArgs() { Property = this, Selected = Selected });
                return true;
            }
            return false;
        }

        public bool CaptureMissing()
        {
            if (Selected && !GotValue)
            {
                Value = Parameter.GetValue(Track ?? 0);
                OnValChanged(new StateChangedEventArgs() { Property = this, Selected = Selected });
                return true;
            }
            return false;
        }

        public bool Restore()
        {
            if (Selected && GotValue)
            {
                Parameter.SetValue(Track ?? 0, Value.Value);
                return true;
            }
            return false;
        }

        public void Clear()
        {
            Value = null;
            OnValChanged(new StateChangedEventArgs() { Property = this, Selected = Selected });
        }

        public void Purge()
        {
            if (GotValue && !Selected)
            {
                Clear();
            }
        }

        public void OnSelChanged(StateChangedEventArgs e)
        {
            EventHandler<StateChangedEventArgs> handler = SelChanged;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        public void OnValChanged(StateChangedEventArgs e)
        {
            EventHandler<StateChangedEventArgs> handler = ValChanged;
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
            Value = new int?();
        }

        public IAttribute Attribute { get; private set; }
        public int? Value { get; set; }

        public string Name => Attribute.Name;

        public int? Track { get; private set; }
        public bool GotValue => Value.HasValue;

        public bool Capture()
        {
            if (Selected)
            {
                Value = Attribute.Value;
                OnValChanged(new StateChangedEventArgs() { Property = this, Selected = Selected });
                return true;
            }
            return false;
        }

        public bool CaptureMissing()
        {
            if (Selected && !GotValue)
            {
                Value = Attribute.Value;
                OnValChanged(new StateChangedEventArgs() { Property = this, Selected = Selected });
                return true;
            }
            return false;
        }

        public bool Restore()
        {
            if (Selected && GotValue)
            {
                Attribute.Value = Value.Value;
                return true;
            }
            return false;
        }

        public void Clear()
        {
            Value = null;
            OnValChanged(new StateChangedEventArgs() { Property = this, Selected = Selected });
        }

        public void Purge()
        {
            if (GotValue && !Selected)
            {
                Clear();
            }
        }

        public bool Selected { get; set; }
        public event EventHandler<StateChangedEventArgs> SelChanged;
        public event EventHandler<StateChangedEventArgs> ValChanged;

        public void OnSelChanged(StateChangedEventArgs e)
        {
            EventHandler<StateChangedEventArgs> handler = SelChanged;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        public void OnValChanged(StateChangedEventArgs e)
        {
            EventHandler<StateChangedEventArgs> handler = ValChanged;
            if (handler != null)
            {
                handler(this, e);
            }
        }
    }

    public class DataState : IPropertyState
    {
        public DataState(IMachine machine)
        {
            _machine = machine;
            Selected = false;
            Value = null;
        }

        public byte [] Value { get; set; }

        public string Name => "Data";

        private IMachine _machine;
        public IMachine Machine => _machine;
        public int? Track => null;
        
        public bool GotValue => Value != null;

        public bool Capture()
        {
            if(Selected)
            {
                Value = Machine.Data;
                OnValChanged(new StateChangedEventArgs() { Property = this, Selected = Selected });
                return true;
            }
            return false;
        }

        public bool CaptureMissing()
        {
            if (Selected && !GotValue)
            {
                Value = Machine.Data;
                OnValChanged(new StateChangedEventArgs() { Property = this, Selected = Selected });
                return true;
            }
            return false;
        }

        public bool Restore()
        {
            if (Selected && GotValue)
            {
                Machine.Data = Value;
                return true;
            }
            return false;
        }

        public void Clear()
        {
            Value = null;
            OnValChanged(new StateChangedEventArgs() { Property = this, Selected = Selected });
        }

        public void Purge()
        {
            if(GotValue && !Selected)
            {
                Clear();
            }
        }

        public bool Selected { get; set; }

        public event EventHandler<StateChangedEventArgs> SelChanged;
        public event EventHandler<StateChangedEventArgs> ValChanged;

        public void OnSelChanged(StateChangedEventArgs e)
        {
            EventHandler<StateChangedEventArgs> handler = SelChanged;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        public void OnValChanged(StateChangedEventArgs e)
        {
            EventHandler<StateChangedEventArgs> handler = ValChanged;
            if (handler != null)
            {
                handler(this, e);
            }
        }
    }

    public class PropertyStateGroup : IGroup<IPropertyState>, IValueContainer
    {
        public PropertyStateGroup(string name)
        {
            Name = name;
            Children = new List<IPropertyState>();
        }

        public string Name { get; }

        public List<IPropertyState> Children { get; }

        public bool GotValue { get; private set; }

        public event EventHandler<StateChangedEventArgs> ValChanged;

        public bool Capture()
        {
            foreach (IPropertyState ps in Children)
            {
                if (ps.Capture()) GotValue = true;
            }
            return GotValue;
        }


        public bool CaptureMissing()
        {
            foreach (IPropertyState ps in Children)
            {
                if (ps.CaptureMissing()) GotValue = true;
            }
            return GotValue;
        }

        public void Clear()
        {
            if (GotValue)
            {
                foreach (IPropertyState ps in Children)
                {
                    ps.Clear();
                }
                GotValue = false;
            }
        }

        public void Purge()
        {
            if (GotValue)
            {
                foreach (IPropertyState ps in Children)
                {
                    ps.Purge();
                }
            }
        }

        public void OnValChanged(StateChangedEventArgs e)
        {
            throw new NotImplementedException();
        }

        public bool Restore()
        {
            bool result = false;
            if (GotValue)
            {
                foreach (IPropertyState ps in Children)
                {
                    if (ps.Restore()) result = true;
                }
            }
            return result;
        }
    }

    public class TrackPropertyStateGroup : IGroup<PropertyStateGroup>, IValueContainer
    {
        public TrackPropertyStateGroup(string name)
        {
            Name = name;
            Children = new List<PropertyStateGroup>();
        }

        public string Name { get; }

        public List<PropertyStateGroup> Children { get; }

        public bool GotValue { get; private set; }

        public event EventHandler<StateChangedEventArgs> ValChanged;

        public bool Capture()
        {
            foreach (PropertyStateGroup pg in Children)
            {
                if (pg.Capture()) GotValue = true;
            }
            return GotValue;
        }

        public bool CaptureMissing()
        {
            foreach (PropertyStateGroup pg in Children)
            {
                if (pg.CaptureMissing()) GotValue = true;
            }
            return GotValue;
        }

        public void Clear()
        {
            if (GotValue)
            {
                foreach (PropertyStateGroup pg in Children)
                {
                    pg.Clear();
                }
                GotValue = false;
            }
        }

        public void OnValChanged(StateChangedEventArgs e)
        {
            throw new NotImplementedException();
        }

        public bool Restore()
        {
            bool result = false;
            if (GotValue)
            {
                foreach (PropertyStateGroup pg in Children)
                {
                    if (pg.Restore()) result = true;
                }
            }
            return result;
        }

        public void Purge()
        {
            bool statesRemaining = false;

            if (GotValue)
            {
                foreach (PropertyStateGroup pg in Children)
                {
                    pg.Purge();
                    if (pg.GotValue) statesRemaining = true;
                }
            }

            GotValue = statesRemaining;
        }
    }

    public class MachineState
    {
        public MachineState(IMachine m)
        {
            Machine = m;
            GotState = false;

            _allProperties = new List<IPropertyState>();

            DataStates = new DataState(m);
            _allProperties.Add(DataStates);

            InputStates = new PropertyStateGroup("Input");
            foreach(var p in Machine.ParameterGroups.Single(x => x.Type == ParameterGroupType.Input).Parameters)
            {
                if(p.Flags.HasFlag(ParameterFlags.State))
                {
                    var ps = new ParameterState(p);
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
                        pg.Children.Add(ps);
                        _allProperties.Add(ps);
                    }
                }
            }

            AttributeStates = new PropertyStateGroup("Attributes");
            foreach (var a in Machine.Attributes)
            {
                var ats = new AttributeState(a);
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
            if(DataStates.Capture()) GotState = true;

            if(InputStates.Capture()) GotState = true;

            if (GlobalStates.Capture()) GotState = true;

            if (TrackStates.Capture()) GotState = true;

            return GotState;
        }

        public bool CaptureMissing()
        {
            if (DataStates.CaptureMissing()) GotState = true;

            if (InputStates.CaptureMissing()) GotState = true;

            if (GlobalStates.CaptureMissing()) GotState = true;

            if (TrackStates.CaptureMissing()) GotState = true;

            return GotState;
        }

        public bool Restore()
        {
            bool result = false;

            if (DataStates.Restore()) result = true;

            if (InputStates.Restore()) result = true;

            if (GlobalStates.Restore()) result = true;

            if (TrackStates.Restore()) result = true;

            return result;
        }

        public void Clear()
        {
            DataStates.Clear();

            InputStates.Clear();

            GlobalStates.Clear();

            TrackStates.Clear();

            GotState = false;
        }

        public void Purge()
        {
            DataStates.Clear();

            InputStates.Clear();

            GlobalStates.Clear();

            TrackStates.Clear();
        }
    }
}
