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
                    NotifyPropertyChanged("DisplayName");
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

        public CMachinePropertyItemVM DataState { get; private set; }
        public CPropertyStateGroupVM AttributeStates { get; private set; }
        public CPropertyStateGroupVM GlobalStates { get; private set; }
        public CTrackPropertyStateGroupVM TrackStates { get; private set; }

        public void RefreshState(bool refreshProperties)
        {
            if (refreshProperties)
            {
                foreach (CPropertyBase p in Properties)
                {
                    p.OnPropertyStateChanged();
                }
            }

            if (AttributeStates != null)
            {
                AttributeStates.NotifyPropertyChanged("GotValue");
            }

            if (GlobalStates != null)
            {
                GlobalStates.NotifyPropertyChanged("GotValue"); 
            }

            if (TrackStates != null)
            {
                foreach(CPropertyStateGroupVM pvm in TrackStates.Children)
                {
                    pvm.NotifyPropertyChanged("GotValue");
                }
                TrackStates.NotifyPropertyChanged("GotValue");
            }

            NotifyPropertyChanged("GotValue");
            NotifyPropertyChanged("HasSmoothing");
        }

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
                Properties.Add(_state.DataState);
                DataState = new CMachinePropertyItemVM(_state.DataState, this, _ownerVM, _viewRef);
                Children.Add(DataState);
                DataState.PropertyChanged += OnChildPropertyChanged;
            }

            if (_state.AttributeStates.ChildProperties.Count > 0)
            {
                Properties.UnionWith(_state.AttributeStates.ChildProperties);
                AttributeStates = new CPropertyStateGroupVM(_state.AttributeStates, this, _ownerVM, _viewRef);
                Children.Add(AttributeStates);
                AttributeStates.PropertyChanged += OnChildPropertyChanged;
            }

            if (_state.GlobalStates.ChildProperties.Count > 0)
            {
                Properties.UnionWith(_state.GlobalStates.ChildProperties);
                GlobalStates = new CPropertyStateGroupVM(_state.GlobalStates, this, _ownerVM, _viewRef);
                Children.Add(GlobalStates);
                GlobalStates.PropertyChanged += OnChildPropertyChanged;
            }

            if (_state.TrackStates.ChildProperties.Count > 0)
            {
                foreach(CPropertyBase p in _state.TrackStates.ChildProperties)
                {
                    Properties.UnionWith(p.ChildProperties);
                }
                TrackStates = new CTrackPropertyStateGroupVM(_state.TrackStates, this, _ownerVM, _viewRef);
                Children.Add(TrackStates);
                TrackStates.PropertyChanged += OnChildPropertyChanged;
            }
        }

        internal void OnTrackCountChanged()
        {
            try
            {
                if (TrackStates == null) return;

                int newCount = _state.Machine.TrackCount;
                int count = TrackStates.Children[0].Children.Count;
                int delta = newCount - count;

                if (delta < 0) // track(s?) removed
                {
                    foreach (CMachinePropertyItemVM param in TrackStates.Children)
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
                        CTreeViewItemVM paramVM = TrackStates.Children[index];
                        CMachinePropertyItemVM pvm = new CMachinePropertyItemVM(property, paramVM, _ownerVM, _viewRef);

                        pvm.PropertyChanged += OnChildPropertyChanged;
                        paramVM.AddChild(pvm);
                        TrackStates.Properties.Add(property);
                        Properties.Add(property);
                        index++;
                    }
                }
            }
            catch(System.NullReferenceException)
            {

            }
        }
    }
}