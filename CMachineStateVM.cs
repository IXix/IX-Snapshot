using BuzzGUI.Common;
using BuzzGUI.Interfaces;
using System.ComponentModel;
using System.Linq;

namespace Snapshot
{
    // Machines
    public class CMachineStateVM : CTreeViewItemVM
    {
        public readonly CMachineState _state;
        public readonly int _trackCount;

        public CMachineStateVM(CMachineState state, CMachineSnapshot reference)
            : base(null, true)
        {
            _state = state;
            _trackCount = _state.Machine.TrackCount;
            state.Machine.PropertyChanged += OnMachinePropertyChanged;
            Reference = reference;
            IsChecked = false;
            LoadChildren();
        }

        private void OnMachinePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var m = sender as IMachine;

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

        public string Name => _state.Machine.Name;

        public CMachineSnapshot Reference { get; internal set; }

        public override bool GotValue
        {
            get
            {
                try
                {
                    IPropertyState v = _state.AllProperties.First(x => Reference.ContainsProperty(x));
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
                var s = new CPropertyStateVM(_state.DataState, this);
                Children.Add(s);
            }

            if (_state.AttributeStates.Children.Count > 0)
            {
                var s = new CPropertyStateGroupVM(_state.AttributeStates, this);
                Children.Add(s);
            }

            if (_state.GlobalStates.Children.Count > 0)
            {
                var s = new CPropertyStateGroupVM(_state.GlobalStates, this);
                Children.Add(s);
            }

            if (_state.TrackStates.Children.Count > 0)
            {
                var s = new CTrackPropertyStateGroupVM(_state.TrackStates, this);
                Children.Add(s);
            }
        }

        internal void OnTrackCountChanged()
        {
            try
            {
                var trackParams = Children.First(x => x.GetType().Name == "CTrackPropertyStateGroupVM");

                int newCount = _state.Machine.TrackCount;
                int count = trackParams.Children[0].Children.Count;
                int delta = newCount - count;

                if (delta < 0) // track(s?) removed
                {
                    foreach (var param in trackParams.Children)
                    {
                        var vm = param.Children[newCount]; // newCount should be index of track to remove
                        param.RemoveChild(vm);
                        param.UpdateTreeCheck();
                    }
                }
                else if (delta > 0) // track(s?) added
                {
                    var track = _state.TrackStates.Children[0].Children.First(x => x.Track == count); // Count should be index of new track
                    foreach (var param in trackParams.Children)
                    {
                        param.AddChild(new CPropertyStateVM(track, param));
                        param.UpdateTreeCheck();
                    }
                }
            }
            catch
            {

            }
        }
    }
}