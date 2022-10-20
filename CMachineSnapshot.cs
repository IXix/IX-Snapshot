﻿using BuzzGUI.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Threading;

namespace Snapshot
{
    public class CMachineSnapshot : INotifyPropertyChanged
    {
        public CMachineSnapshot(CMachine owner, int index)
        {
            m_owner = owner;
            Index = index;
            Name = string.Format("Slot {0}", Index);
            AttributeValues = new Dictionary<CAttributeState, int>();
            ParameterValues = new Dictionary<CParameterState, Tuple<int, int>>();
            DataValues = new Dictionary<CDataState, byte[]>();
            StoredProperties = new List<IPropertyState>();
        }

        public CMachineSnapshot(CMachineSnapshot src)
        {
            m_owner = src.m_owner;
            Name = src.Name + " copy";
            Index = src.Index;
            AttributeValues = new Dictionary<CAttributeState, int>(src.AttributeValues);
            ParameterValues = new Dictionary<CParameterState, Tuple<int, int>>(src.ParameterValues);
            DataValues = new Dictionary<CDataState, byte[]>(src.DataValues);
            StoredProperties = new List<IPropertyState>(src.StoredProperties);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public virtual void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        private readonly CMachine m_owner;
        internal Dictionary<CAttributeState, int /*value*/> AttributeValues;
        internal Dictionary<CParameterState, Tuple<int /*track*/, int /*value*/>> ParameterValues;
        internal Dictionary<CDataState, byte[] /*value*/> DataValues;
        public List<IPropertyState> StoredProperties { get; internal set; }

        public int Index { get; private set; }

        private string _name;
        public string Name
        {
            get => _name;
            set
            {
                if(value != _name)
                {
                    _name = value;
                    OnPropertyChanged("Name");
                }
            }
        }

        public bool HasData => StoredProperties.Count() > 0;

        public bool ContainsProperty(IPropertyState p)
        {
            return StoredProperties.Exists(x => x == p);
        }

        public bool ContainsMachine(CMachineState s)
        {
            return StoredProperties.Exists(x => x.Parent == s);
        }

        public void SetPropertyValue(CPropertyBase p, int value)
        {
            switch (p.GetType().Name)
            {
                case "CParameterState":
                    {
                        var key = p as CParameterState;
                        try
                        {
                            var track = ParameterValues[key].Item1;
                            ParameterValues[key] = new Tuple<int, int>(track, value) ;
                        }
                        catch
                        {
                            var track = key.Track.HasValue ? p.Track.Value : -1;
                            ParameterValues.Add(key, new Tuple<int, int>(track, value));
                            StoredProperties.Add(p);
                        }
                    }
                    break;

                case "CAttributeState":
                    {
                        // FIXME
                    }
                    break;

                case "CDataState":
                    // FIXME: Careful. Null to signify empty. Anyhting else is dodgy.
                    return;

                default:
                    break;
            }
        }

        public string GetPropertyDisplayValue(IPropertyState p)
        {
            if (!ContainsProperty(p)) return "";

            switch (p.GetType().Name)
            {
                case "CParameterState":
                    return " (" + ParameterValues.First(x => x.Key == p).Value.Item2.ToString() + ")";

                case "CAttributeState":
                    return " (" + AttributeValues.First(x => x.Key == p).Value.ToString() + ")";

                case "CDataState":
                    return " (" + DataValues[(p as CDataState)].Length.ToString() + ")";

                default:
                    return "Wuh??";
            }
        }

        public int GetPropertySize(CPropertyBase p)
        {
            if (!ContainsProperty(p)) return 0;
            
            switch(p.GetType().Name)
            {
                case "CDataState":
                    return DataValues[(p as CDataState)].Length;

                default:
                    return sizeof(int);
            }
        }

        public int Size
        {
            get
            {
                int size = 0;
                size += AttributeValues.Count * sizeof(int);
                size += ParameterValues.Count * sizeof(int);
                foreach (KeyValuePair<CDataState, byte[]> s in DataValues)
                {
                    size += s.Value.Length;
                }
                return size;
            }
        }

        // How many properties have been captured
        public int StoredCount => StoredProperties.Count;

        // How many properties are stored that aren't selected
        public int RedundantCount => StoredProperties.Count(x => x.Active && x.Selected == false);

        // How many properties are stored that are inactive (machine deleted)
        public int DeletedCount => StoredProperties.Count(x => x.Active == false);

        public void CopyFrom(CMachineSnapshot src)
        {
            foreach(KeyValuePair<CAttributeState, int> p in src.AttributeValues)
            {
                if (p.Value == -1)
                {
                    AttributeValues.Remove(p.Key);
                }
                else
                {
                    try
                    {
                        AttributeValues.Add(p.Key, p.Value);
                        StoredProperties.Add(p.Key);
                    }
                    catch
                    {
                        AttributeValues[p.Key] = p.Value;
                    }
                }
            }

            foreach (KeyValuePair<CParameterState, Tuple<int, int>> p in src.ParameterValues)
            {
                if (p.Value.Item2 == -1)
                {
                    ParameterValues.Remove(p.Key);
                }
                else
                {
                    try
                    {
                        ParameterValues.Add(p.Key, p.Value);
                        StoredProperties.Add(p.Key);
                    }
                    catch
                    {
                        ParameterValues[p.Key] = p.Value;
                    }
                }
            }

            foreach(var p in src.DataValues)
            {
                if (p.Value == null)
                {
                    DataValues.Remove(p.Key);
                }
                else
                {
                    try
                    {
                        DataValues.Add(p.Key, p.Value);
                        StoredProperties.Add(p.Key);
                    }
                    catch
                    {
                        DataValues[p.Key] = p.Value;
                    }
                }
            }
        }

        public void Capture()
        {
            DoClear();

            foreach (CMachineState state in m_owner.States)
            {
                foreach (CAttributeState s in state.AttributeStates.Children.Where(x => x.Selected))
                {
                    AttributeValues.Add(s, s.Attribute.Value);
                    StoredProperties.Add(s);
                }

                foreach (CParameterState s in state.GlobalStates.Children.Where(x => x.Selected))
                {
                    ParameterValues.Add(s, new Tuple<int, int>(-1, s.Parameter.GetValue(-1)));
                    StoredProperties.Add(s);
                }

                foreach (CPropertyStateGroup pg in state.TrackStates.Children)
                {
                    foreach (CParameterState s in pg.Children.Where(x => x.Selected))
                    {
                        ParameterValues.Add(s, new Tuple<int, int>(s.Track.Value, s.Parameter.GetValue(s.Track.Value)));
                        StoredProperties.Add(s);
                    }
                }

                if (state.DataState != null && state.DataState.Selected)
                {
                    DataValues.Add(state.DataState, state.DataState.Machine.Data);
                    StoredProperties.Add(state.DataState);
                }
            }

            OnPropertyChanged("HasData");
        }

        public void CaptureMissing()
        {
            foreach (CMachineState state in m_owner.States)
            {
                foreach (CAttributeState s in state.AttributeStates.Children.Where(x => x.Selected))
                {
                    try
                    {
                        int? value = AttributeValues[s];
                    }
                    catch
                    {
                        AttributeValues.Add(s, s.Attribute.Value);
                        StoredProperties.Add(s);
                    }
                }

                foreach (CParameterState s in state.GlobalStates.Children.Where(x => x.Selected))
                {
                    try
                    {
                        Tuple<int, int> value = ParameterValues[s];
                    }
                    catch
                    {
                        ParameterValues.Add(s, new Tuple<int, int>(-1, s.Parameter.GetValue(-1)));
                        StoredProperties.Add(s);
                    }
                }

                foreach (CPropertyStateGroup pg in state.TrackStates.Children)
                {
                    foreach (CParameterState s in pg.Children.Where(x => x.Selected))
                    {
                        try
                        {
                            Tuple<int, int> value = ParameterValues[s];
                        }
                        catch
                        {
                            ParameterValues.Add(s, new Tuple<int, int>(s.Track.Value, s.Parameter.GetValue(s.Track.Value)));
                            StoredProperties.Add(s);
                        }
                    }
                }

                if (state.DataState != null && state.DataState.Selected)
                {
                    try
                    {
                        var value = DataValues[state.DataState];
                    }
                    catch
                    {
                        DataValues.Add(state.DataState, state.DataState.Machine.Data);
                        StoredProperties.Add(state.DataState);
                    }
                }
            }
            OnPropertyChanged("HasData");
        }

        public void Restore()
        {
            Application.Current.Dispatcher.BeginInvoke(
                (Action)(() =>
                {
                    foreach (var v in AttributeValues.Where(x => x.Key.Active))
                    {
                        v.Key.Attribute.Value = v.Value;
                    }
                    foreach (var v in ParameterValues.Where(x => x.Key.Active))
                    {
                        v.Key.Parameter.SetValue(v.Value.Item1, v.Value.Item2);
                    }
                    foreach (var v in DataValues.Where(x => x.Key.Active))
                    {
                        v.Key.Machine.Data = v.Value;
                    }
                }),
                DispatcherPriority.Send
                );
        }

        public void Purge()
        {
            // Could use List.Except() but these lists are readonly and not sure if bindings etc. would be preserved
            // Need testing at some point
            // list = list.Except(list.Where(x => x.Key.Selected == false));

            // Confirm if necessary
            if (m_owner.ConfirmClear)
            {
                string msg = string.Format("Discard {0} stored properties?", StoredProperties.Count(x => x.Selected == false || x.Active == false));
                MessageBoxResult result = MessageBox.Show(msg, "Confirm purge", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.No)
                {
                    return;
                }
            }

            var attrList = AttributeValues.Where(x => x.Key.Active == false || x.Key.Selected == false).ToList();
            foreach (KeyValuePair<CAttributeState, int> item in attrList)
            {
                CAttributeState p = item.Key;
                AttributeValues.Remove(p);
                StoredProperties.Remove(p);
            }
            var paraList = ParameterValues.Where(x => x.Key.Active == false || x.Key.Selected == false).ToList();
            foreach (KeyValuePair<CParameterState, Tuple<int, int>> item in paraList)
            {
                CParameterState p = item.Key;
                ParameterValues.Remove(p);
                StoredProperties.Remove(p);
            }
            var dataList = DataValues.Where(x => x.Key.Active == false || x.Key.Selected == false).ToList();
            foreach (KeyValuePair<CDataState, byte[]> item in dataList)
            {
                CDataState p = item.Key;
                DataValues.Remove(p);
                StoredProperties.Remove(p);
            }

            OnPropertyChanged("HasData");
        }

        internal void RemoveProperty(IPropertyState p)
        {
            if (!ContainsProperty(p)) return;
            
            switch (p.GetType().Name)
            {
                case "CParameterState":
                    {
                        var key = p as CParameterState;
                        ParameterValues.Remove(key);
                    }
                    break;

                case "CAttributeState":
                    {
                        var key = p as CAttributeState;
                        AttributeValues.Remove(key);
                    }
                    break;

                case "CDataState":
                    {
                        var key = p as CDataState;
                        DataValues.Remove(key);
                    }
                    break;

                default:
                    return;
            }
            StoredProperties.Remove(p);
        }

        private void DoClear()
        {
            AttributeValues.Clear();
            ParameterValues.Clear();
            DataValues.Clear();
            StoredProperties.Clear();
            OnPropertyChanged("HasData");
        }

        public void Clear()
        {
            // Confirm if necessary
            if (m_owner.ConfirmClear)
            {
                MessageBoxResult result = MessageBox.Show("Discard all stored properties", "Confirm clear", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.No)
                {
                    return;
                }
            }

            DoClear();
        }

        private void DoClearSelected()
        {
            foreach(var p in AttributeValues.Keys.Where(x => x.Selected))
            {
                AttributeValues.Remove(p);
            }

            foreach (var p in ParameterValues.Keys.Where(x => x.Selected))
            {
                ParameterValues.Remove(p);
            }

            foreach (var p in DataValues.Keys.Where(x => x.Selected))
            {
                DataValues.Remove(p);
            }

            StoredProperties.RemoveAll(x => x.Selected);
            OnPropertyChanged("HasData");
        }

        public void ClearSelected()
        {
            // Confirm if necessary
            if (m_owner.ConfirmClear)
            {
                MessageBoxResult result = MessageBox.Show("Discard all stored properties", "Confirm clear", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.No)
                {
                    return;
                }
            }

            DoClearSelected();
        }

        public void ReadProperty(IPropertyState p, BinaryReader r)
        {
            Type t = p.GetType();
            switch (t.FullName)
            {
                case "Snapshot.CDataState":
                    Int32 l = r.ReadInt32();
                    DataValues[p as Snapshot.CDataState] = r.ReadBytes(l);
                    break;

                case "Snapshot.CAttributeState":
                    AttributeValues[p as Snapshot.CAttributeState] = r.ReadInt32();
                    break;

                case "Snapshot.CParameterState":
                    ParameterValues[p as Snapshot.CParameterState] = new Tuple<int, int>(p.Track ?? -1, r.ReadInt32());
                    break;

                default:
                    throw new Exception("Unknown property type.");
            }
            StoredProperties.Add(p);
        }

        public void WriteProperty(CPropertyBase p, BinaryWriter w)
        {
            if (ContainsProperty(p) && p.Active)
            {
                Type t = p.GetType();
                switch (t.FullName)
                {
                    case "Snapshot.CDataState":
                        byte[] data = DataValues[p as Snapshot.CDataState];
                        w.Write(data.Length);
                        w.Write(data);
                        break;

                    case "Snapshot.CAttributeState":
                        w.Write(AttributeValues[p as Snapshot.CAttributeState]);
                        break;

                    case "Snapshot.CParameterState":
                        w.Write(ParameterValues[p as Snapshot.CParameterState].Item2); // Value
                        break;

                    default:
                        throw new Exception("Unknown property type.");
                }
            }
        }

        public void WriteData(BinaryWriter w)
        {
            w.Write(Name);
        }

        public void ReadData(BinaryReader r)
        {
            Name = r.ReadString();
        }

        public override string ToString()
        {
            return Name;
        }
    }
}