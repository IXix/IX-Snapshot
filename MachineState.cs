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
    public class Misc
    {
        // Byte count to formatted string solution - https://stackoverflow.com/a/48467634
        private static string[] suffixes = new[] { "b", "K", "M", "G", "T", "P" };
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
        int Slot { get; set; }
        Int32 Size { get; }
        Int32 TotalSize { get; }

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
        bool NonEmpty { get; }
        bool ReadData(BinaryReader r, Byte version);
        bool WriteData(BinaryWriter w);
    }

    public interface IGroup<T> : INamed
    {
        List<T> Children { get; }
    }

    public class PropertyBase : IPropertyState
    {
        public PropertyBase()
        {
            Selected = false;
            Slot = 0;
            Track = null;
        }

        virtual public int? Track { get; protected set; }

        virtual public bool Selected { get; set; }

        virtual public string Name => throw new NotImplementedException();

        virtual public bool GotValue { get; protected set; }

        virtual public bool NonEmpty => throw new NotImplementedException();

        virtual public int Size => throw new NotImplementedException();

        virtual public int TotalSize => throw new NotImplementedException();

        virtual public bool ReadData(BinaryReader r, Byte version)
        {
            /* Note:
             * Not reading name here, used by the caalling
             * routine to find the correct property.
             */

            int? track = r.ReadInt32();
            if (track == -1) track = null;

            if (track != Track) throw new Exception("Data mismatch");

            Selected = r.ReadBoolean();

            return r.ReadBoolean(); // non-empty, more stuff to load
        }

        virtual public bool WriteData(BinaryWriter w)
        {
            bool nonEmpty = NonEmpty;

            w.Write(Name);
            w.Write((Int32)(Track ?? -1));
            w.Write(Selected);
            w.Write(nonEmpty);

            return nonEmpty;
        }

        protected int m_slot;
        virtual public int Slot
        {
            get => m_slot;
            set
            {
                if (m_slot != value)
                {
                    m_slot = value;
                    OnSelChanged(new StateChangedEventArgs() { Property = this, Selected = Selected });
                    OnValChanged(new StateChangedEventArgs() { Property = this, Selected = Selected });
                }
            }
        }

        public event EventHandler<StateChangedEventArgs> SelChanged;
        public void OnSelChanged(StateChangedEventArgs e)
        {
            EventHandler<StateChangedEventArgs> handler = SelChanged;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        public event EventHandler<StateChangedEventArgs> ValChanged;
        public void OnValChanged(StateChangedEventArgs e)
        {
            EventHandler<StateChangedEventArgs> handler = ValChanged;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        virtual public bool Capture()
        {
            throw new NotImplementedException();
        }

        virtual public bool CaptureMissing()
        {
            throw new NotImplementedException();
        }

        virtual public void Clear()
        {
            throw new NotImplementedException();
        }

        virtual public void Purge()
        {
            throw new NotImplementedException();
        }

        virtual public bool Restore()
        {
            throw new NotImplementedException();
        }
    }

    public class ParameterState : PropertyBase
    {
        public ParameterState(IParameter param, int? track = null)
        {
            Parameter = param;
            Track = track;
            m_values = new int?[128];
        }

        public IParameter Parameter { get; private set; }

        private int?[] m_values;
        public int? Value
        {
            get => m_values[Slot];
            set => m_values[Slot] = value;
        }

        public override string Name => Track == null ?Parameter.Name : Track.ToString();

        public override bool GotValue => Value.HasValue;

        public override bool NonEmpty => m_values.Count(x => x.HasValue) > 0;

        public override int Size => Value.HasValue ? Marshal.SizeOf(Value) : 0;

        public override int TotalSize
        {
            get
            {
                int size = 0;
                for(int i = 0; i <  m_values.Length; i++)
                {
                    size += m_values[i] != null ? Marshal.SizeOf(m_values[i]) : 0;
                }
                return size;
            }
        }

        public override bool ReadData(BinaryReader r, Byte version)
        {
            bool result = false;
            if (base.ReadData(r, version))
            {
                result = true;
                Int32 count = r.ReadInt32(); // Number of filled slots
                for (Int32 c = 0; c < count; c++)
                {
                    Byte i = r.ReadByte();
                    m_values[i] = r.ReadInt32();
                }

                OnValChanged(new StateChangedEventArgs() { Property = this, Selected = Selected });
                OnSelChanged(new StateChangedEventArgs() { Property = this, Selected = Selected });
            }
            return result;
        }

        public override bool WriteData(BinaryWriter w)
        {
            bool result = false;
            if (base.WriteData(w))
            {
                result = true;
                Int32 count = m_values.Count(x => x.HasValue); // Number of filled slots
                w.Write(count);
                for (Byte i = 0; i < 128; i++)
                {
                    if (m_values[i].HasValue)
                    {
                        w.Write(i);
                        w.Write((Int32)m_values[i].Value);
                        result = true;
                    }
                }
            }
            return result;
        }

        public override bool Capture()
        {
            if(Selected)
            {
                Value = Parameter.GetValue(Track ?? 0);
                OnValChanged(new StateChangedEventArgs() { Property = this, Selected = Selected });
                return true;
            }
            return false;
        }

        public override bool CaptureMissing()
        {
            if (Selected && !GotValue)
            {
                Value = Parameter.GetValue(Track ?? 0);
                OnValChanged(new StateChangedEventArgs() { Property = this, Selected = Selected });
                return true;
            }
            return false;
        }

        public override bool Restore()
        {
            if (Selected && GotValue)
            {
                int t = Track ?? 0;
                int v = Value.Value;
                Application.Current.Dispatcher.BeginInvoke((Action)(() =>
                {
                    Parameter.SetValue(t, v);
                }), DispatcherPriority.Send
                );
                return true;
            }
            return false;
        }

        public override void Clear()
        {
            Value = null;
            OnValChanged(new StateChangedEventArgs() { Property = this, Selected = Selected });
        }

        public override void Purge()
        {
            if (GotValue && !Selected)
            {
                Clear();
            }
        }
    }

    public class AttributeState : PropertyBase
    {
        public AttributeState(IAttribute attr)
        {
            Attribute = attr;
            m_values = new int?[128];
        }

        public IAttribute Attribute { get; private set; }

        private int?[] m_values;
        public int? Value
        {
            get => m_values[Slot];
            set => m_values[Slot] = value;
        }

        public override string Name => Attribute.Name;

        public override bool GotValue => Value.HasValue;

        public override bool NonEmpty => m_values.Count(x => x.HasValue) > 0;

        public override int Size => Value.HasValue ? Marshal.SizeOf(Value) : 0;

        public override int TotalSize
        {
            get
            {
                int size = 0;
                for (int i = 0; i < m_values.Length; i++)
                {
                    size += m_values[i] != null ? Marshal.SizeOf(m_values[i]) : 0;
                }
                return size;
            }
        }

        public override bool ReadData(BinaryReader r, Byte version)
        {
            bool result = false;
            if (base.ReadData(r, version))
            {
                result = true;
                Int32 count = r.ReadInt32(); // Number of filled slots
                for (Int32 c = 0; c < count; c++)
                {
                    Byte i = r.ReadByte();
                    m_values[i] = r.ReadInt32();
                }

                OnValChanged(new StateChangedEventArgs() { Property = this, Selected = Selected });
                OnSelChanged(new StateChangedEventArgs() { Property = this, Selected = Selected });
            }
            return result;
        }

        public override bool WriteData(BinaryWriter w)
        {
            bool result = false;
            if (base.WriteData(w))
            {
                result = true;
                Int32 count = m_values.Count(x => x.HasValue); // Number of filled slots
                w.Write(count);
                for (Byte i = 0; i < 128; i++)
                {
                    if (m_values[i].HasValue)
                    {
                        w.Write(i);
                        w.Write((Int32)m_values[i].Value);
                        result = true;
                    }
                }
            }
            return result;
        }

        public override bool Capture()
        {
            if (Selected)
            {
                Value = Attribute.Value;
                OnValChanged(new StateChangedEventArgs() { Property = this, Selected = Selected });
                return true;
            }
            return false;
        }

        public override bool CaptureMissing()
        {
            if (Selected && !GotValue)
            {
                Value = Attribute.Value;
                OnValChanged(new StateChangedEventArgs() { Property = this, Selected = Selected });
                return true;
            }
            return false;
        }

        public override bool Restore()
        {
            if (Selected && GotValue)
            {
                int v = Value.Value;
                Application.Current.Dispatcher.BeginInvoke((Action)(() =>
                {
                    Attribute.Value = v;
                }), DispatcherPriority.Send
                );
            }
            return false;
        }

        public override void Clear()
        {
            Value = null;
            OnValChanged(new StateChangedEventArgs() { Property = this, Selected = Selected });
        }

        public override void Purge()
        {
            if (GotValue && !Selected)
            {
                Clear();
            }
        }
    }

    public class DataState : PropertyBase
    {
        public DataState(IMachine machine)
        {
            _machine = machine;
            m_values = new byte[128][];
        }

        private byte[][] m_values;
        public byte[] Value
        {
            get => m_values[Slot];
            set => m_values[Slot] = value;
        }

        public override string Name => "Data";

        private IMachine _machine;
        public IMachine Machine => _machine;

        public override int? Track => null;

        public override bool GotValue => Value != null;

        public override bool NonEmpty => m_values.Count(x => x != null) > 0;

        public override int Size => Value != null ? Value.Length : 0;

        public override int TotalSize
        {
            get
            {
                int size = 0;
                for (int i = 0; i < m_values.Length; i++)
                {
                    size += m_values[i] != null ? m_values[i].Length : 0;
                }
                return size;
            }
        }

        public override bool ReadData(BinaryReader r, Byte version)
        {
            bool result = false;
            if (base.ReadData(r, version))
            {
                result = true;
                Int32 count = r.ReadInt32(); // Number of filled slots
                for (Int32 c = 0; c < count; c++)
                {
                    Byte i = r.ReadByte();
                    Int32 size = r.ReadInt32();
                    m_values[i] = r.ReadBytes(size);
                }

                OnValChanged(new StateChangedEventArgs() { Property = this, Selected = Selected });
                OnSelChanged(new StateChangedEventArgs() { Property = this, Selected = Selected });
            }
            return result;
        }

        public override bool WriteData(BinaryWriter w)
        {
            bool result = false;
            if (base.WriteData(w))
            {
                result = true;
                Int32 count = m_values.Count(x => x != null); // Number of filled slots
                w.Write(count);
                for (Byte i = 0; i < 128; i++)
                {
                    if (m_values[i] != null)
                    {
                        w.Write(i);
                        w.Write((Int32)m_values[i].Length);
                        w.Write(m_values[i]);
                        result = true;
                    }
                }
            }
            return result;
        }

        public override bool Capture()
        {
            if(Selected)
            {
                Value = Machine.Data;
                OnValChanged(new StateChangedEventArgs() { Property = this, Selected = Selected });
                return true;
            }
            return false;
        }

        public override bool CaptureMissing()
        {
            if (Selected && !GotValue)
            {
                Value = Machine.Data;
                OnValChanged(new StateChangedEventArgs() { Property = this, Selected = Selected });
                return true;
            }
            return false;
        }

        public override bool Restore()
        {
            if (Selected && GotValue)
            {
                byte [] v = Value;
                Application.Current.Dispatcher.BeginInvoke((Action)(() =>
                {
                    Machine.Data = v;
                }), DispatcherPriority.Send);
            }
            return false;
        }

        public override void Clear()
        {
            Value = null;
            OnValChanged(new StateChangedEventArgs() { Property = this, Selected = Selected });
        }

        public override void Purge()
        {
            if(GotValue && !Selected)
            {
                Clear();
            }
        }
    }

    public class PropertyStateGroup : PropertyBase, IGroup<IPropertyState>
    {
        public PropertyStateGroup(string name)
        {
            Name = name;
            Children = new List<IPropertyState>();
        }

        public override string Name { get; }

        public override bool GotValue
        {
            get { return Children.Count(x => x.GotValue) > 0; }
        }

        public List<IPropertyState> Children { get; }

        public override int Slot
        {
            get => m_slot;
            set
            {
                if (m_slot != value)
                {
                    m_slot = value;
                    foreach(IPropertyState s in Children)
                    {
                        s.Slot = value;
                    }
                }
            }
        }

        public override bool Capture()
        {
            foreach (IPropertyState ps in Children)
            {
                ps.Capture();
            }
            return GotValue;
        }

        public override bool CaptureMissing()
        {
            foreach (IPropertyState ps in Children)
            {
                ps.CaptureMissing();
            }
            return GotValue;
        }

        public override void Clear()
        {
            if (GotValue)
            {
                foreach (IPropertyState ps in Children)
                {
                    ps.Clear();
                }
            }
        }

        public override void Purge()
        {
            bool statesRemaining = false;
            if (GotValue)
            {
                foreach (IPropertyState ps in Children)
                {
                    ps.Purge();
                }
            }
            GotValue = statesRemaining;
        }

        public override bool Restore()
        {
            bool result = false;
            if (GotValue)
            {
                foreach (IPropertyState ps in Children)
                {
                    ps.Restore();
                }
            }
            return result;
        }
    }

    public class TrackPropertyStateGroup : PropertyBase, IGroup<PropertyStateGroup>
    {
        public TrackPropertyStateGroup(string name)
        {
            Name = name;
            Slot = 0;
            Children = new List<PropertyStateGroup>();
        }

        public override string Name { get; }

        public override bool GotValue
        {
            get { return Children.Count(x => x.GotValue) > 0; }
        }

        public List<PropertyStateGroup> Children { get; }

        public override int Slot
        {
            get => m_slot;
            set
            {
                if (m_slot != value)
                {
                    m_slot = value;
                    foreach (PropertyStateGroup g in Children)
                    {
                        g.Slot = value;
                    }
                }
            }
        }

        public override bool Capture()
        {
            foreach (PropertyStateGroup pg in Children)
            {
                pg.Capture();
            }
            return GotValue;
        }

        public override bool CaptureMissing()
        {
            foreach (PropertyStateGroup pg in Children)
            {
                pg.CaptureMissing();
            }
            return GotValue;
        }

        public override void Clear()
        {
            if (GotValue)
            {
                foreach (PropertyStateGroup pg in Children)
                {
                    pg.Clear();
                }
            }
        }

        public override bool Restore()
        {
            bool result = false;
            if (GotValue)
            {
                foreach (PropertyStateGroup pg in Children)
                {
                    pg.Restore();
                }
            }
            return result;
        }

        public override void Purge()
        {
            if (GotValue)
            {
                foreach (PropertyStateGroup pg in Children)
                {
                    pg.Purge();
                }
            }
        }
    }

    public class MachineSnapshot
    {
        public MachineSnapshot()
        {
            ParamValues = new Dictionary<Tuple<IParameter, int>, int>();
            DataValues = new Dictionary<IMachine, byte[]>();
        }

        // Collect any stored values so we can give them to Buzz in one shot
        public void AddState(MachineState state)
        {
            foreach (ParameterState ps in state.GlobalStates.Children.Where(x => x.GotValue))
            {
                ParamValues.Add(new Tuple<IParameter,int>(ps.Parameter, 0), ps.Value.Value);
            }

            foreach (PropertyStateGroup pg in state.TrackStates.Children.Where(x => x.GotValue))
            {
                foreach (ParameterState ps in pg.Children.Where(x => x.GotValue))
                {
                    ParamValues.Add(new Tuple<IParameter, int>(ps.Parameter, ps.Track.Value), ps.Value.Value);
                }
            }

            if(state.DataStates != null && state.DataStates.GotValue)
            {
                DataValues.Add(state.Machine, state.DataStates.Value);
            }
        }

        public void Apply()
        {
            foreach(var v in ParamValues)
            {
                v.Key.Item1.SetValue(v.Key.Item2, v.Value);
            }
            foreach (var v in DataValues)
            {
                v.Key.Data = v.Value;
            }
        }

        readonly Dictionary<Tuple<IParameter, int /*track*/>, int /*value*/> ParamValues;
        readonly Dictionary<IMachine, byte[]> DataValues;
    }

    public class MachineState
    {
        public MachineState(IMachine m)
        {
            Machine = m;
            Slot = 0;

            _allProperties = new List<IPropertyState>();

            if((Machine.DLL.Info.Flags & MachineInfoFlags.LOAD_DATA_RUNTIME) == MachineInfoFlags.LOAD_DATA_RUNTIME && Machine.Data != null)
            {
                DataStates = new DataState(m);
                _allProperties.Add(DataStates);
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

        // True if anything is stored in current slot
        public bool GotState => _allProperties.Count(x => x.GotValue) > 0;

        public int SlotSize
        {
            get
            {
                int v = 0;
                foreach(IPropertyState s in _allProperties)
                {
                    v += s.Size;
                }
                return v;
            }
        }

        public int TotalSize
        {
            get
            {
                int v = 0;
                foreach (IPropertyState s in _allProperties)
                {
                    v += s.TotalSize;
                }
                return v;
            }
        }

        private int m_slot;
        public int Slot
        {
            get => m_slot;
            set
            {
                if(m_slot != value)
                {
                    m_slot = value;
                    if (DataStates != null) DataStates.Slot = value;
                    if (GlobalStates != null) GlobalStates.Slot = value;
                    if (TrackStates != null) TrackStates.Slot = value;
                    if (AttributeStates != null) AttributeStates.Slot = value;
                }
            }
        }

        // How many selected properies have not been captured
        public int SelCount
        {
            get { return _allProperties.Count(x => x.Selected == true); }
        }

        // How many properties have been captured
        public int StoredCount
        {
            get { return _allProperties.Count(x => x.GotValue == true); }
        }

        // How many properties are stored that aren't selected
        public int RedundantCount
        {
            get { return _allProperties.Count(x => x.Selected == false && x.GotValue == true); }
        }

        // How many selected properties have not been captured
        public int MissingCount
        {
            get { return _allProperties.Count(x => x.Selected == true && x.GotValue == false); }
        }

        // Storage. The publics are for the treeview
        // the private is to make capture etc. simpler
        public DataState DataStates { get; set; }
        public PropertyStateGroup GlobalStates { get; private set; }
        public TrackPropertyStateGroup TrackStates { get; private set; }
        public PropertyStateGroup AttributeStates { get; private set; }
        private readonly List<IPropertyState> _allProperties;
        public List<IPropertyState> AllProperties { get { return _allProperties; } }

        public bool Capture()
        {
            if(DataStates != null) DataStates.Capture();

            AttributeStates.Capture();

            GlobalStates.Capture();

            TrackStates.Capture();

            return GotState;
        }

        public bool CaptureMissing()
        {
            if (DataStates != null) DataStates.CaptureMissing();

            AttributeStates.CaptureMissing();

            GlobalStates.CaptureMissing();

            TrackStates.CaptureMissing();

            return GotState;
        }

        public bool Restore()
        {
            bool result = false;
            if (GotState)
            {
                if (DataStates != null && DataStates.Restore()) result = true;

                if (AttributeStates.Restore()) result = true;

                if (GlobalStates.Restore()) result = true;

                if (TrackStates.Restore()) result = true;
            }
            return result;
        }

        public void Clear()
        {
            if(DataStates != null) DataStates.Clear();

            AttributeStates.Clear();

            GlobalStates.Clear();

            TrackStates.Clear();
        }

        public void Purge()
        {
            if (DataStates != null) DataStates.Purge();

            AttributeStates.Purge();

            GlobalStates.Purge();

            TrackStates.Purge();
        }
    }
}
