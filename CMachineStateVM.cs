using BuzzGUI.Common;
using BuzzGUI.Interfaces;
using System;
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
            : base(null, true, ownerVM, view)
        {
            _state = state;
            _trackCount = _state.Machine.TrackCount;
            _properties = _state.AllProperties;
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

        public override int? SmoothingCount => _state.SmoothingCount;

        public override int? SmoothingUnits => _state.SmoothingUnits;

        protected override void LoadChildren()
        {
            if (_state.DataState != null)
            {
                CPropertyStateVM s = new CPropertyStateVM(_state.DataState, this, _ownerVM, _viewRef);
                Children.Add(s);
                s.PropertyChanged += OnChildPropertyChanged;
            }

            if (_state.AttributeStates.Children.Count > 0)
            {
                CPropertyStateGroupVM s = new CPropertyStateGroupVM(_state.AttributeStates, this, _ownerVM, _viewRef);
                s.PropertyChanged += OnChildPropertyChanged;
                Children.Add(s);
            }

            if (_state.GlobalStates.Children.Count > 0)
            {
                CPropertyStateGroupVM s = new CPropertyStateGroupVM(_state.GlobalStates, this, _ownerVM, _viewRef);
                s.PropertyChanged += OnChildPropertyChanged;
                Children.Add(s);
            }

            if (_state.TrackStates.Children.Count > 0)
            {
                CTrackPropertyStateGroupVM s = new CTrackPropertyStateGroupVM(_state.TrackStates, this, _ownerVM, _viewRef);
                s.PropertyChanged += OnChildPropertyChanged;
                Children.Add(s);
            }
        }

        private void OnChildPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(e.PropertyName);
        }

        internal void OnTrackCountChanged()
        {
            try
            {
                CTreeViewItemVM trackParams = Children.First(x => x.GetType().Name == "CTrackPropertyStateGroupVM");

                int newCount = _state.Machine.TrackCount;
                int count = trackParams.Children[0].Children.Count;
                int delta = newCount - count;

                if (delta < 0) // track(s?) removed
                {
                    foreach (CTreeViewItemVM param in trackParams.Children)
                    {
                        CTreeViewItemVM vm = param.Children[newCount]; // newCount should be index of track to remove
                        param.RemoveChild(vm);
                        param.UpdateTreeCheck();
                    }
                }
                else if (delta > 0) // track(s?) added
                {
                    int index = 0;
                    foreach(CPropertyStateGroup param in _state.TrackStates.Children)
                    {
                        IPropertyState property = param.Children.First(x => x.Track == count); // Count should be index of new track
                        CTreeViewItemVM paramVM = trackParams.Children[index];
                        paramVM.AddChild(new CPropertyStateVM(property, paramVM, _ownerVM, _viewRef));
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