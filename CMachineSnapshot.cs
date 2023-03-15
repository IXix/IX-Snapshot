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
            Notes = "";
            AttributeValues = new Dictionary<CAttributeState, int>();
            ParameterValues = new Dictionary<CParameterState, Tuple<int, int>>();
            DataValues = new Dictionary<CDataState, byte[]>();
            StoredProperties = new HashSet<CPropertyBase>();
        }

        public CMachineSnapshot(CMachineSnapshot src)
        {
            m_owner = src.m_owner;
            Name = src.Name + " copy";
            Index = src.Index;
            AttributeValues = new Dictionary<CAttributeState, int>(src.AttributeValues);
            ParameterValues = new Dictionary<CParameterState, Tuple<int, int>>(src.ParameterValues);
            DataValues = new Dictionary<CDataState, byte[]>(src.DataValues);
            StoredProperties = new HashSet<CPropertyBase>(src.StoredProperties);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private readonly CMachine m_owner;
        internal Dictionary<CAttributeState, int /*value*/> AttributeValues;
        internal Dictionary<CParameterState, Tuple<int /*track*/, int /*value*/>> ParameterValues;
        internal Dictionary<CDataState, byte[] /*value*/> DataValues;
        public HashSet<CPropertyBase> StoredProperties { get; internal set; }

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
            return StoredProperties.Contains(p);
        }

        public bool ContainsMachine(CMachineState s)
        {
            return StoredProperties.Count(x => x.ParentMachine == s) > 0;
        }

        public void SetPropertyValue(CPropertyBase p, int? value)
        {
            // null == clear
            if (value == null)
            {
                if (StoredProperties.Contains(p))
                {
                    CAttributeState ak = p as CAttributeState;
                    if (ak != null)
                    {
                        AttributeValues.Remove(ak);
                    }

                    CParameterState pk = p as CParameterState;
                    if (pk != null)
                    {
                        ParameterValues.Remove(pk);
                    }

                    _ = StoredProperties.Remove(p);

                    p.OnPropertyStateChanged();
                    OnPropertyChanged("HasData");
                }

                return;
            }

            switch (p.GetType().Name)
            {
                case "CAttributeState":
                    {
                        CAttributeState key = p as CAttributeState;
 
                        value = Math.Min(Math.Max((int) value, key.Attribute.MinValue), key.Attribute.MaxValue);
                        AttributeValues[key] = (int) value;
                    }
                    break;

                case "CParameterState":
                    {
                        CParameterState key = p as CParameterState;
                        int track = p.Track ?? -1;
                        value = Math.Min(Math.Max((int)value, key.Parameter.MinValue), key.Parameter.MaxValue);
                        ParameterValues[key] = new Tuple<int, int>(track, (int) value);
                    }
                    break;


                case "CDataState":
                    return; // Bad idea to set machine data manually

                default:
                    throw new Exception("Unknown property type.");
            }

            if(!StoredProperties.Contains(p))
            {
                _ = StoredProperties.Add(p);
            }

            p.OnPropertyStateChanged();
            OnPropertyChanged("HasData");
        }


        public int? GetPropertyValue(IPropertyState p)
        {
            if (!ContainsProperty(p)) return null;

            switch (p.GetType().Name)
            {
                case "CParameterState":
                    return ParameterValues[p as CParameterState].Item2;

                case "CAttributeState":
                    return AttributeValues[p as CAttributeState];

                case "CDataState":
                    return null;

                default:
                    return null;
            }
        }

        public string GetPropertyValueString(IPropertyState p)
        {
            if (!ContainsProperty(p)) return "";

            string value;

            switch (p.GetType().Name)
            {
                case "CParameterState":
                    {
                        CParameterState key = p as CParameterState;
                        int v = ParameterValues[key].Item2;
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

            return value;
        }

        public string GetPropertyDisplayValue(IPropertyState p)
        {
            return " (" + GetPropertyValueString(p) + ")";
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
        public int SelectedCount => StoredProperties.Count(x => x.Checked == true);
        public int SelectedCount_M => StoredProperties.Count(x => x.Checked_M == true);

        // How many properties are stored that aren't selected
        public int RedundantCount => StoredProperties.Count(x => x.Active && x.Checked == false);

        public int RedundantCount_M => StoredProperties.Count(x => x.Active && x.Checked_M == false);

        // How many properties are stored that are inactive (machine deleted)
        public int DeletedCount => StoredProperties.Count(x => x.Active == false);

        // So you can remember what the this snapshot has in it and why, because you're getting old.
        public string Notes { get; internal set; }

        public void CopyFrom(CMachineSnapshot src)
        {
            foreach(IPropertyState p in src.StoredProperties)
            {
                switch (p.GetType().Name)
                {
                    case "CAttributeState":
                        {
                            CAttributeState key = p as CAttributeState;
                            AttributeValues[key] = src.AttributeValues[key];
                        }
                        break;

                    case "CParameterState":
                        {
                            CParameterState key = p as CParameterState;
                            ParameterValues[key] = src.ParameterValues[key];
                        }
                        break;

                    case "CDataState":
                        {
                            CDataState key = p as CDataState;
                            DataValues[key] = src.DataValues[key];
                        }
                        break;

                    default:
                        throw new Exception("Unknown property type.");
                }
            }

            StoredProperties.UnionWith(src.StoredProperties);

            OnPropertyChanged("HasData");
        }

        public void CopyFrom(HashSet<CPropertyBase> properties, CMachineSnapshot src)
        {
            foreach (CPropertyBase p in properties)
            {
                switch (p.GetType().Name)
                {
                    case "CAttributeState":
                        {
                            CAttributeState key = p as CAttributeState;
                            try
                            {
                                AttributeValues[key] = src.AttributeValues[key];
                                _ = StoredProperties.Add(p);
                            }
                            catch
                            {
                                _ = AttributeValues.Remove(key);
                                _ = StoredProperties.Remove(p);
                            }
                        }
                        break;

                    case "CParameterState":
                        {
                            CParameterState key = p as CParameterState;
                            try
                            {
                                ParameterValues[key] = src.ParameterValues[key];
                                _ = StoredProperties.Add(p);
                            }
                            catch
                            {
                                _ = ParameterValues.Remove(key);
                                _ = StoredProperties.Remove(p);
                            }
                        }
                        break;

                    case "CDataState":
                        {
                            CDataState key = p as CDataState;
                            try
                            {
                                DataValues[key] = src.DataValues[key];
                                _ = StoredProperties.Add(p);
                            }
                            catch
                            {
                                _ = DataValues.Remove(key);
                                _ = StoredProperties.Remove(p);
                            }
                        }
                        break;

                    default:
                        throw new Exception("Unknown property type.");
                }
            }

            OnPropertyChanged("HasData");
        }

        public void Capture(CPropertyBase p)
        {
            switch (p.GetType().Name)
            {
                case "CAttributeState":
                    {
                        CAttributeState key = p as CAttributeState;
                        AttributeValues[key] = key.Attribute.Value;
                    }
                    break;

                case "CParameterState":
                    {
                        CParameterState key = p as CParameterState;
                        int track = key.Track ?? -1;
                        ParameterValues[key] = new Tuple<int, int>(track, key.Parameter.GetValue(track));
                    }
                    break;

                case "CDataState":
                    {
                        CDataState key = p as CDataState;
                        DataValues[key] = key.Machine.Data;
                    }
                    break;

                default:
                    throw new Exception("Unknown property type.");
            }

            _ = StoredProperties.Add(p);

            p.OnPropertyStateChanged();

            OnPropertyChanged("HasData");
        }

        public void Capture(HashSet<CPropertyBase> targets, bool clearExisting)
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
                            CAttributeState key = p as CAttributeState;
                            AttributeValues[key] = key.Attribute.Value;
                        }
                        break;

                    case "CParameterState":
                        {
                            CParameterState key = p as CParameterState;
                            int track = key.Track ?? -1;
                            ParameterValues[key] = new Tuple<int, int>(track, key.Parameter.GetValue(track));
                        }
                        break;

                    case "CDataState":
                        {
                            CDataState key = p as CDataState;
                            DataValues[key] = key.Machine.Data;
                        }
                        break;

                    default:
                        throw new Exception("Unknown property type.");
                }
            }

            StoredProperties.UnionWith(targets);

            foreach(CPropertyBase p in targets)
            {
                p.OnPropertyStateChanged();
            }

            OnPropertyChanged("HasData");
        }

        public void Restore(HashSet<CPropertyBase> properties)
        {
            foreach (IPropertyState p in properties)
            {
                if (!StoredProperties.Contains(p) || !p.Active) return;

                switch (p.GetType().Name)
                {
                    case "CAttributeState":
                        {
                            CAttributeState key = p as CAttributeState;
                            int value = AttributeValues[key];
                            m_owner.RegisterAttribChange(key.Attribute, value, true);
                        }
                        break;

                    case "CParameterState":
                        {
                            CParameterState key = p as CParameterState;
                            Tuple<int, int> t = ParameterValues[key];
                            int track = t.Item1;
                            int value = t.Item2;
                            m_owner.RegisterParamChange(key, track, value, true);
                        }
                        break;

                    case "CDataState":
                        {
                            CDataState key = p as CDataState;
                            byte[] value = DataValues[key];
                            _ = Application.Current.Dispatcher.BeginInvoke(
                                (Action)(() => { key.Machine.Data = value; }
                                ),
                                DispatcherPriority.Send
                                );
                        }
                        break;

                    default:
                        throw new Exception("Unknown property type.");
                }
            }
        }

        public void Restore()
        {
            lock (m_owner.changeLock)
            {
                m_owner.ClearPendingChanges();

                foreach (KeyValuePair<CAttributeState, int> v in AttributeValues.Where(x => x.Key.Active))
                {
                    m_owner.RegisterAttribChange(v.Key.Attribute, v.Value);
                }

                foreach (KeyValuePair<CParameterState, Tuple<int, int>> v in ParameterValues.Where(x => x.Key.Active))
                {
                    m_owner.RegisterParamChange(v.Key, v.Value.Item1, v.Value.Item2);
                }
            }

            // Data has to be changed via the main thread
            _ = Application.Current.Dispatcher.BeginInvoke(
                (Action)(() =>
                {
                    lock (m_owner.changeLock)
                    {
                        foreach (KeyValuePair<CDataState, byte[]> v in DataValues.Where(x => x.Key.Active))
                        {
                            v.Key.Machine.Data = v.Value;
                        }
                    }
                }),
                DispatcherPriority.Send
                );
        }

        public void Purge(bool main)
        {
            HashSet<CPropertyBase> removers;
            if (main)
            {
                removers = StoredProperties.Where(x => x.Active == false || x.Checked == false).ToHashSet();
                Remove(removers);
            }
            else
            {
                removers = StoredProperties.Where(x => x.Active == false || x.Checked_M == false).ToHashSet();
                Remove(removers);
            }

            foreach(CPropertyBase p in removers.Where(x => x.Active))
            {
                p.OnPropertyStateChanged();
            }

            OnPropertyChanged("HasData");
        }

        public void Remove(CPropertyBase p)
        {
            if (!StoredProperties.Contains(p)) return;

            switch (p.GetType().Name)
            {
                case "CAttributeState":
                    {
                        CAttributeState key = p as CAttributeState;
                        _ = AttributeValues.Remove(key);
                    }
                    break;

                case "CParameterState":
                    {
                        CParameterState key = p as CParameterState;
                        _ = ParameterValues.Remove(key);
                    }
                    break;

                case "CDataState":
                    {
                        CDataState key = p as CDataState;
                        _ = DataValues.Remove(key);
                    }
                    break;

                default:
                    throw new Exception("Unknown property type.");
            }

            _ = StoredProperties.Remove(p);

            p.OnPropertyStateChanged();
            OnPropertyChanged("HasData");
        }


        public void Remove(HashSet<CPropertyBase> targets)
        {
            foreach (IPropertyState p in targets)
            {
                switch (p.GetType().Name)
                {
                    case "CAttributeState":
                        {
                            CAttributeState key = p as CAttributeState;
                            _ = AttributeValues.Remove(key);
                        }
                        break;

                    case "CParameterState":
                        {
                            CParameterState key = p as CParameterState;
                            _ = ParameterValues.Remove(key);
                        }
                        break;

                    case "CDataState":
                        {
                            CDataState key = p as CDataState;
                            _ = DataValues.Remove(key);
                        }
                        break;

                    default:
                        throw new Exception("Unknown property type.");
                }
            }

            StoredProperties = StoredProperties.Except(targets).ToHashSet();

            foreach (CPropertyBase p in targets)
            {
                p.OnPropertyStateChanged();
            }
            OnPropertyChanged("HasData");
        }

        internal void Clear()
        {
            HashSet<CPropertyBase> removed = new HashSet<CPropertyBase>(StoredProperties);

            AttributeValues.Clear();
            ParameterValues.Clear();
            DataValues.Clear();
            StoredProperties.Clear();

            foreach(CPropertyBase p in removed)
            {
                p.OnPropertyStateChanged();
            }
            OnPropertyChanged("HasData");
        }

        public void ReadProperty(CPropertyBase p, BinaryReader r)
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
            _ = StoredProperties.Add(p);
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