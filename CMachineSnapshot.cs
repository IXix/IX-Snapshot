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
            m_selection = new CPropertySelection(owner, true);
            Index = index;
            Name = string.Format("Slot {0}", Index);
            Notes = "";
            AttributeValues = new Dictionary<CAttributeState, int>();
            ParameterValues = new Dictionary<CParameterState, Tuple<int, int>>();
            DataValues = new Dictionary<CDataState, byte[]>();
            StoredProperties = new HashSet<CPropertyBase>();
            MidiInfo = new CMidiBindingInfo();
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

        public CMidiBindingInfo MidiInfo;

        private readonly CPropertySelection m_selection;
        internal HashSet<CPropertyBase> Selection => m_selection.SelectedProperties;

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

        public bool CanPurge => HasData && RedundantCount > 0;

        public bool ContainsProperty(IPropertyState p)
        {
            return StoredProperties.Contains(p);
        }

        public bool ContainsMachine(CMachineState s)
        {
            return StoredProperties.Count(x => x.ParentMachine == s) > 0;
        }

        public bool SelectionContainsMachine(CMachineState s)
        {
            return m_selection.SelectedProperties.Count(x => x.ParentMachine == s) > 0;
        }

        public void SetPropertyValue(CPropertyBase p, int? value)
        {
            // null == clear
            if (value == null)
            {
                if (StoredProperties.Contains(p))
                {
                    if (p is CAttributeState ak)
                    {
                        AttributeValues.Remove(ak);
                    }

                    if (p is CParameterState pk)
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
            string s = GetPropertyValueString(p);

            if(s != "")
                s = " (" + s +")";

            return s;
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
        public int SelectedCount => Selection.Count;
        public int SelectedCount_M => m_owner.SelectionM.Count;

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

                    case "CPropertyStateGroup":
                        break;

                    case "CTrackPropertyStateGroup":
                        break;

                    default:
                        throw new Exception("Unknown property type.");
                }
            }

            OnPropertyChanged("HasData");
        }

        public void Capture()
        {
            Capture(m_owner.Selection, true);
        }

        public void Capture(CPropertyBase p, bool clearExisting)
        {
            if (clearExisting)
            {
                Clear(false);
            }

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

                case "CPropertyStateGroup":
                    return;

                case "CTrackPropertyStateGroup":
                    return;

                case "CMachineState":
                    return;

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
                Clear(false);
            }

            if(targets == null)
            {
                targets = m_owner.Selection;
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

                    case "CPropertyStateGroup":
                        break;

                    case "CTrackPropertyStateGroup":
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

        // Dummy bool is to avoid having a separate CMidiAction subclass just for this method
        public void CaptureMissing(HashSet<CPropertyBase> targets, bool dummy = false)
        {
            if (targets == null)
            {
                targets = m_owner.Selection;
            }

            HashSet<CPropertyBase> missing = targets.Where(x => ContainsProperty(x) == false).ToHashSet();
            Capture(missing, false);
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

        internal bool Confirm(string title, string msg)
        {
            MessageBoxResult result = MessageBox.Show(msg, title, MessageBoxButton.YesNo);
            if (result == MessageBoxResult.No)
            {
                return false;
            }
            return true;
        }

        public void Purge(HashSet<CPropertyBase> selection, bool confirm)
        {
            if (selection == null)
            {
                selection = m_owner.Selection;
            }

            HashSet<CPropertyBase> removers = StoredProperties.Where(x => x.Active == false || selection.Contains(x) == false).ToHashSet();

            if(removers.Count == 0)
            {
                return;
            }

            bool proceed = true;

            if (confirm)
            {
                proceed = Confirm("Confirm Purge", string.Format("Remove {0} stored properties?", removers.Count));
            }
            
            if(proceed)
            {
                Remove(removers);

                foreach(CPropertyBase p in removers.Where(x => x.Active))
                {
                    p.OnPropertyStateChanged();
                }

                OnPropertyChanged("HasData");
            }
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
            if (targets == null)
            {
                targets = m_owner.Selection;
            }

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

        public void Clear(bool confirm)
        {
            bool proceed = true;
            if(confirm)
            {
                proceed = Confirm("Clear", string.Format("Remove {0} stored properties?", StoredCount));
            }

            if (proceed)
            {
                HashSet<CPropertyBase> removed = new HashSet<CPropertyBase>(StoredProperties);

                AttributeValues.Clear();
                ParameterValues.Clear();
                DataValues.Clear();
                StoredProperties.Clear();

                foreach (CPropertyBase p in removed)
                {
                    p.OnPropertyStateChanged();
                }
                OnPropertyChanged("HasData");
            }
        }

        public void ClearSelected(HashSet<CPropertyBase> targets, bool confirm)
        {
            bool proceed = true;

            if(confirm)
            {
                proceed = Confirm("Clear Properties", string.Format("Remove {0} stored properties?", targets.Count));
            }

            if(proceed)
            {
                Remove(targets);
            }
        }

        public void ReadPropertyValue(CPropertyBase p, BinaryReader r)
        {
            // File version 3 always stored data. Version 4 might not .
            bool loadValue = true;
            if (m_owner.LoadVersion >= 4)
            {
                loadValue = r.ReadBoolean();
            }

            if (loadValue)
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
        }

        public void WritePropertyValue(CPropertyBase p, BinaryWriter w)
        {
            if (ContainsProperty(p) && p.Active)
            {
                w.Write(true);

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
            else
            {
                w.Write(false);
            }
        }

        public void WriteData(BinaryWriter w)
        {
            w.Write(Name);
            w.Write(Notes);
        }

        public void ReadData(BinaryReader r)
        {
            Byte file_version = m_owner.LoadVersion;

            Name = r.ReadString();

            if(file_version >= 3)
            {
                Notes = r.ReadString();
            }
        }

        public override string ToString()
        {
            return Name;
        }
    }
}