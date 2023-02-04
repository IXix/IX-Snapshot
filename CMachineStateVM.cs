using BuzzGUI.Common;
using BuzzGUI.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Snapshot
{
    // Machines
    public class CMachineStateVM : CMachinePropertyItemVM
    {
        public readonly CMachineState _state;
        public readonly int _trackCount;

        public CMachineStateVM(CMachineState state, CSnapshotMachineVM ownerVM, int view)
            : base(state, null, ownerVM, view)
        {
            _state = state;
            _trackCount = _state.Machine.TrackCount;
            OwnerVM = ownerVM;

            state.Machine.PropertyChanged += OnMachinePropertyChanged;
            LoadChildren();
        }

        private void OnMachinePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            IMachine m = sender as IMachine;

            if (m != _state.Machine) return;

            switch (e.PropertyName)
            {
                case "Name":
                    //FIXME
                    break;

                case "TrackCount":
                    OnTrackCountChanged();
                    break;

                default:
                    // "Attributes":
                    // "IsBypassed":
                    // "IsMuted":
                    // "IsSoloed":
                    // "IsActive":
                    // "IsWireless":
                    // "LastEngineThread" :
                    // "MIDIInputChannel":
                    // "OversampleFactor":
                    // "OverrideLatency":
                    // "Patterns":
                    // "PatternEditorDLL":
                    // "Position":
                    break;
            }
        }

        public override string Name => _state.Machine.Name;

        internal CSnapshotMachineVM OwnerVM { get; set; }

        private CMachinePropertyItemVM _dataVM;
        private CPropertyStateGroupVM _attrVM;
        private CPropertyStateGroupVM _globalsVM;
        private CTrackPropertyStateGroupVM _tracksVM;

        public override bool GotValue
        {
            get
            {
                try
                {
                    _ = _state.AllProperties.First(x => ReferenceSlot().ContainsProperty(x));
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        protected override void LoadChildren()
        {
            if (_state.DataState != null)
            {
                _dataVM = new CMachinePropertyItemVM(_state.DataState, this, _ownerVM, _viewRef);
                Children.Add(_dataVM);
            }

            if (_state.AttributeStates.ChildProperties.Count > 0)
            {
                _attrVM = new CPropertyStateGroupVM(_state.AttributeStates, this, _ownerVM, _viewRef);
                Children.Add(_attrVM);
            }

            if (_state.GlobalStates.ChildProperties.Count > 0)
            {
                _globalsVM = new CPropertyStateGroupVM(_state.GlobalStates, this, _ownerVM, _viewRef);
                Children.Add(_globalsVM);
            }

            if (_state.TrackStates.ChildProperties.Count > 0)
            {
                _tracksVM = new CTrackPropertyStateGroupVM(_state.TrackStates, this, _ownerVM, _viewRef);
                Children.Add(_tracksVM);
            }
        }

        internal void OnTrackCountChanged()
        {
            try
            {
                int newCount = _state.Machine.TrackCount;
                int count = _tracksVM.Children[0].Children.Count;
                int delta = newCount - count;

                if (delta < 0) // track(s?) removed
                {
                    foreach (CMachinePropertyItemVM param in _tracksVM.Children)
                    {
                        CTreeViewItemVM vm = param.Children[newCount]; // newCount should be index of track to remove
                        param.RemoveChild(vm);
                        param.UpdateTreeCheck();
                    }
                }
                else if (delta > 0) // track(s?) added
                {
                    int index = 0;
                    foreach(CPropertyBase param in _state.TrackStates.ChildProperties)
                    {
                        CPropertyBase property = param.ChildProperties.First(x => x.Track == count); // Count should be index of new track
                        CTreeViewItemVM paramVM = _tracksVM.Children[index];
                        paramVM.AddChild(new CMachinePropertyItemVM(property, paramVM, _ownerVM, _viewRef));
                        index++;
                    }
                }
            }
            catch
            {

            }
        }
    }
}