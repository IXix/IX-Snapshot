using BuzzGUI.Interfaces;
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
                case "CAttributeState":
                    {
                        var key = p as CAttributeState;
                        AttributeValues[key] = value;
                    }
                    break;

                case "CParameterState":
                    {
                        var key = p as CParameterState;
                        int track = p.Track ?? -1;
                        ParameterValues[key] = new Tuple<int, int>(track, value) ;
                    }
                    break;


                case "CDataState":
                    return; // Bad idea to set machine data manually

                default:
                    throw new Exception("Unknown property type.");
            }

            if(!StoredProperties.Contains(p))
            {
                StoredProperties.Add(p);
            }
        }

        public string GetPropertyDisplayValue(IPropertyState p)
        {
            if (!ContainsProperty(p)) return "";

            string value;

            switch (p.GetType().Name)
            {
                case "CParameterState":
                    {
                        var key = p as CParameterState;
                        var v = ParameterValues[key].Item2;
                        value = key.Parameter.DescribeValue(v);
                    }
                    break;

                case "CAttributeState":
                    {
                        value = AttributeValues[p as CAttributeState].ToString();
                    }
                    break;

                case "CDataState":
                    value = DataValues[p as CDataState].Length.ToString();
                    break;

                default:
                    throw new Exception("Unknown property type.");
            }

            return " (" + value + ")";
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

        // How many stored properties are selected
        public int SelectedCount => StoredProperties.Count(x => x.Selected);
        public int SelectedCount_M => StoredProperties.Count(x => x.Selected_M);

        // How many properties are stored that aren't selected
        public int RedundantCount => StoredProperties.Count(x => x.Active && x.Selected == false);

        public int RedundantCount_M => StoredProperties.Count(x => x.Active && x.Selected_M == false);

        // How many properties are stored that are inactive (machine deleted)
        public int DeletedCount => StoredProperties.Count(x => x.Active == false);

        public void CopyFrom(CMachineSnapshot src)
        {
            foreach(IPropertyState p in src.StoredProperties)
            {
                switch (p.GetType().Name)
                {
                    case "CAttributeState":
                        {
                            var key = p as CAttributeState;
                            AttributeValues[key] = src.AttributeValues[key];
                        }
                        break;

                    case "CParameterState":
                        {
                            var key = p as CParameterState;
                            ParameterValues[key] = src.ParameterValues[key];
                        }
                        break;

                    case "CDataState":
                        {
                            var key = p as CDataState;
                            DataValues[key] = src.DataValues[key];
                        }
                        break;

                    default:
                        throw new Exception("Unknown property type.");
                }
            }

            StoredProperties = StoredProperties.Union(src.StoredProperties).ToList();
        }

        public void Capture(List<IPropertyState> targets, bool clearExisting)
        {
            if(clearExisting)
            {
                Clear();
            }

            foreach (IPropertyState p in targets)
            {
                switch (p.GetType().Name)
                {
                    case "CAttributeState":
                        {
                            var key = p as CAttributeState;
                            AttributeValues[key] = key.Attribute.Value;
                        }
                        break;

                    case "CParameterState":
                        {
                            var key = p as CParameterState;
                            int track = key.Track ?? -1;
                            ParameterValues[key] = new Tuple<int, int>(track, key.Parameter.GetValue(track));
                        }
                        break;

                    case "CDataState":
                        {
                            var key = p as CDataState;
                            DataValues[key] = key.Machine.Data;
                        }
                        break;

                    default:
                        throw new Exception("Unknown property type.");
                }
            }

            StoredProperties = StoredProperties.Union(targets).ToList();

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

        public void Purge(bool main)
        {
            if (main)
            {
                Remove(StoredProperties.Where(x => x.Active == false || x.Selected == false).ToList());
            }
            else
            {
                Remove(StoredProperties.Where(x => x.Active == false || x.Selected_M == false).ToList());
            }
            OnPropertyChanged("HasData");
        }

        public void Remove(List<IPropertyState> targets)
        {
            foreach (IPropertyState p in targets)
            {
                switch (p.GetType().Name)
                {
                    case "CAttributeState":
                        {
                            var key = p as CAttributeState;
                            AttributeValues.Remove(key);
                        }
                        break;

                    case "CParameterState":
                        {
                            var key = p as CParameterState;
                            ParameterValues.Remove(key);
                        }
                        break;

                    case "CDataState":
                        {
                            var key = p as CDataState;
                            DataValues.Remove(key);
                        }
                        break;

                    default:
                        throw new Exception("Unknown property type.");
                }
            }

            StoredProperties = StoredProperties.Except(targets).ToList();

            OnPropertyChanged("HasData");
        }

        internal void Clear()
        {
            AttributeValues.Clear();
            ParameterValues.Clear();
            DataValues.Clear();
            StoredProperties.Clear();
            OnPropertyChanged("HasData");
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