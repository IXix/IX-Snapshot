using BuzzGUI.Interfaces;
using System;
using System.Collections.Generic;
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
            _allProperties = owner.AllProperties;
        }

        private readonly CMachine m_owner;
        readonly Dictionary<CAttributeState, int /*value*/> AttributeValues;
        readonly Dictionary<CParameterState, Tuple<int /*track*/, int /*value*/>> ParameterValues;
        readonly Dictionary<CDataState, byte[] /*value*/> DataValues;
        private List<IPropertyState> _allProperties;

        public int Index { get; private set; }

        public string Name { get; set; }

        public bool HasData => (AttributeValues.Count + ParameterValues.Count + DataValues.Count) > 0;

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
        public int StoredCount => AttributeValues.Count + ParameterValues.Count + DataValues.Count;

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
                }

                foreach (CParameterState s in state.GlobalStates.Children.Where(x => x.Selected))
                {
                    ParameterValues.Add(s, new Tuple<int, int>(-1, s.Parameter.GetValue(-1)));
                }

                foreach (CPropertyStateGroup pg in state.TrackStates.Children.Where(x => x.Selected))
                {
                    foreach (CParameterState s in pg.Children.Where(x => x.Selected))
                    {
                        ParameterValues.Add(s, new Tuple<int, int>(s.Track.Value, s.Parameter.GetValue(s.Track.Value)));
                    }
                }

                if (state.DataState != null && state.DataState.Selected)
                {
                    DataValues.Add(state.DataState, state.DataState.Machine.Data);
                }
            }
        }

        internal void CaptureMissing()
        {
            throw new NotImplementedException(); // FIXME
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
            throw new NotImplementedException(); // FIXME
        }

        public void Clear()
        {
            AttributeValues.Clear();
            ParameterValues.Clear();
            DataValues.Clear();
        }
    }
}