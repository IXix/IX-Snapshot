using BuzzGUI.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Snapshot
{
    public class CMachineSnapshot
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

        private readonly CMachine m_owner;
        readonly Dictionary<CAttributeState, int /*value*/> AttributeValues;
        readonly Dictionary<CParameterState, Tuple<int /*track*/, int /*value*/>> ParameterValues;
        readonly Dictionary<CDataState, byte[] /*value*/> DataValues;
        public List<IPropertyState> StoredProperties { get; private set; }

        public int Index { get; private set; }

        public string Name { get; set; }

        public bool HasData => (AttributeValues.Count + ParameterValues.Count + DataValues.Count) > 0;

        public bool ContainsProperty(IPropertyState p)
        {
            return StoredProperties.Exists(x => x == p);
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
        public int RedundantCount
        {
            get
            {
                return AttributeValues.Count(x => x.Key.Selected == false) +
                       ParameterValues.Count(x => x.Key.Selected == false) +
                       DataValues.Count(x => x.Key.Selected == false);
            }
        }

        public void Capture()
        {
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
        }

        internal void CaptureMissing()
        {
            foreach (CMachineState state in m_owner.States)
            {
                foreach (CAttributeState s in state.AttributeStates.Children.Where(x => x.Selected))
                {
                    try
                    {
                        int value = AttributeValues[s];
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
        }

        public void Restore()
        {
            foreach (var v in AttributeValues)
            {
                v.Key.Attribute.Value = v.Value;
            }
            foreach (var v in ParameterValues)
            {
                v.Key.Parameter.SetValue(v.Value.Item1, v.Value.Item2);
            }
            foreach (var v in DataValues)
            {
                v.Key.Machine.Data = v.Value;
            }
        }

        internal void Purge()
        {
            // Could use List.Except() but these lists are readonly and not sure if bindings etc. would be preserved
            // Need testing at some point
            // list = list.Except(list.Where(x => x.Key.Selected == false));

            var attrList = AttributeValues.Where(x => x.Key.Selected == false).ToList();
            foreach(KeyValuePair<CAttributeState, int> item in attrList)
            {
                CAttributeState p = item.Key;
                AttributeValues.Remove(p);
                StoredProperties.Remove(p);
            }
            var paraList = ParameterValues.Where(x => x.Key.Selected == false).ToList();
            foreach (KeyValuePair<CParameterState, Tuple<int, int>> item in paraList)
            {
                CParameterState p = item.Key;
                ParameterValues.Remove(p);
                StoredProperties.Remove(p);
            }
            var dataList = DataValues.Where(x => x.Key.Selected == false).ToList();
            foreach (KeyValuePair<CDataState, byte[]> item in dataList)
            {
                CDataState p = item.Key;
                DataValues.Remove(p);
                StoredProperties.Remove(p);
            }
        }

        public void Clear()
        {
            AttributeValues.Clear();
            ParameterValues.Clear();
            DataValues.Clear();
            StoredProperties.Clear();
        }

        internal void WriteData(BinaryWriter w)
        {
            throw new NotImplementedException();
        }

        internal void ReadData(BinaryReader r)
        {
            throw new NotImplementedException();
        }
    }
}