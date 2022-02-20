using BuzzGUI.Common;
using BuzzGUI.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Snapshot
{
    // Butchered from https://www.codeproject.com/Articles/26288/Simplifying-the-WPF-TreeView-by-Using-the-ViewMode

    /// <summary>
    /// Base class for all ViewModel classes displayed by TreeViewItems.  
    /// This acts as an adapter between a raw data object and a TreeViewItem.
    /// </summary>
    public class CTreeViewItemVM : INotifyPropertyChanged
    {
        #region Data

        readonly ObservableCollection<CTreeViewItemVM> _children;
        readonly CTreeViewItemVM _parent;

        bool _isExpanded;
        bool _isSelected;
        bool? _isChecked;
        bool _preventManualIndeterminate;

        #endregion // Data

        #region Constructors

        protected CTreeViewItemVM(CTreeViewItemVM parent, bool preventManualIndeterminate)
        {
            _parent = parent;
            _preventManualIndeterminate = preventManualIndeterminate;
            _children = new ObservableCollection<CTreeViewItemVM>();
        }

        #endregion // Constructors

        #region Presentation Members

        /// <summary>
        /// Returns the logical child items of this object.
        /// </summary>
        public ObservableCollection<CTreeViewItemVM> Children
        {
            get { return _children; }
        }

        /// <summary>
        /// Gets/sets whether the TreeViewItem 
        /// associated with this object is expanded.
        /// </summary>
        public bool IsExpanded
        {
            get { return _isExpanded; }
            set
            {
                if (value != _isExpanded)
                {
                    _isExpanded = value;
                    OnPropertyChanged("IsExpanded");
                }

                // Expand all the way up to the root.
                if (_isExpanded && _parent != null)
                    _parent.IsExpanded = true;
            }
        }

        /// <summary>
        /// Gets/sets whether the TreeViewItem 
        /// associated with this object is selected.
        /// </summary>
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (value != _isSelected)
                {
                    _isSelected = value;
                    OnPropertyChanged("IsSelected");
                }
            }
        }

        /// <summary>
        /// If necessary, prevent user click causing indeterminate state.
        /// </summary>
        internal void OnClick()
        {
            if (_preventManualIndeterminate && _isChecked == null)
            {
                IsChecked = false; // force this and any children to be unchecked.
            }
        }

        /// <summary>
        /// Gets/sets whether the TreeViewItem 
        /// associated with this object is checked, unchecked or undetermined.
        /// </summary>
        private bool reentrancyCheck = false;
        public bool? IsChecked
        {
            get { return _isChecked; }
            set
            {
                if (value != _isChecked)
                {
                    if (reentrancyCheck) return;

                    reentrancyCheck = true;

                    _isChecked = value;

                    if(value != null)
                    {

                        if (Children != null)
                        {
                            foreach (var child in Children)
                            {
                                child.IsChecked = _isChecked;
                            }
                        }
                    }

                    if (Parent != null)
                    {
                        Parent.UpdateTreeCheck();
                    }

                    OnPropertyChanged("IsChecked");

                    OnCheckChanged();

                    reentrancyCheck = false;
                }
            }
        }

        public virtual bool GotValue
        {
            get => false;
        }

        internal void UpdateTreeCheck()
        {
            var c = Children.Count(x => x.IsChecked == true);
            var i = Children.Count(x => x.IsChecked == null);

            if(c == 0 && i == 0)
            {
                IsChecked = false;
            }
            else if (c == Children.Count)
            {
                IsChecked = true;
            }
            else
            {
                IsChecked = null;
            }
        }

        protected void OnSelChanged(object sender, StateChangedEventArgs e)
        {
            IsChecked = e.Selected;
        }

        /// <summary>
        /// Invoked when the child items need to be loaded on demand.
        /// Subclasses can override this to populate the Children collection.
        /// </summary>
        protected virtual void LoadChildren()
        {
        }

        protected virtual void OnCheckChanged()
        {

        }

        public CTreeViewItemVM Parent
        {
            get { return _parent; }
        }

        #endregion // Presentation Members

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        public virtual void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                foreach(var c in Children)
                {
                    c.OnPropertyChanged(propertyName);
                }
            }
        }

        internal bool RemoveChild(CTreeViewItemVM VM)
        {
            if(Children.Remove(VM))
                return true;

            foreach(var c in Children)
            {
                if (c.RemoveChild(VM))
                    return true;
            }

            return false;
        }

        internal void AddChild(CTreeViewItemVM VM)
        {
            if(!Children.Contains(VM))
            {
                Children.Add(VM);
            }
        }

        #endregion // INotifyPropertyChanged Members
    }

    // Main interaction for GUI
    public class CSnapshotMachineVM : INotifyPropertyChanged
    {
        public CSnapshotMachineVM(CMachine owner)
        {
            Owner = owner;
            States = new ObservableCollection<CMachineStateVM>(
                (from state in Owner.States
                 select new CMachineStateVM(state))
                .ToList());

            Owner.PropertyChanged += OwnerPropertyChanged;

            cmdCapture = new SimpleCommand
            {
                CanExecuteDelegate = x => true,
                ExecuteDelegate = x => Owner.Capture()
            };
            cmdCaptureMissing = new SimpleCommand
            {
                CanExecuteDelegate = x => true,
                ExecuteDelegate = x => Owner.CaptureMissing()
            };
            cmdRestore = new SimpleCommand
            {
                CanExecuteDelegate = x => true,
                ExecuteDelegate = x => Owner.Restore()
            };
            cmdClear = new SimpleCommand
            {
                CanExecuteDelegate = x => true,
                ExecuteDelegate = x => Owner.Clear()
            };
            cmdPurge = new SimpleCommand
            {
                CanExecuteDelegate = x => true,
                ExecuteDelegate = x => Owner.Purge()
            };
            cmdMap = new SimpleCommand
            {
                ExecuteDelegate = x => Owner.MapCommand(x.ToString(), false)
            };
            cmdMapSpecific = new SimpleCommand
            {
                ExecuteDelegate = x => Owner.MapCommand(x.ToString(), true)
            };
            cmdSelectAll = new SimpleCommand
            {
                ExecuteDelegate = x => Owner.SelectAll()
            };
            cmdSelectNone = new SimpleCommand
            {
                ExecuteDelegate = x => Owner.SelectNone()
            };
            cmdSelectStored = new SimpleCommand
            {
                ExecuteDelegate = x => Owner.SelectStored()
            };
            cmdSelectInvert = new SimpleCommand
            {
                ExecuteDelegate = x => Owner.SelectInvert()
            };
        }

        private void OwnerPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch(e.PropertyName)
            {
                case "State":
                    NotifyPropertyChanged("CurrentSlot");
                    NotifyPropertyChanged("SlotName");
                    NotifyPropertyChanged("SelectionInfo");
                    foreach (CMachineStateVM s in States)
                    {
                        s.OnPropertyChanged("GotValue");
                    }
                    break;

                case "Names":
                    foreach (CMachineStateVM s in States)
                    {
                        s.OnPropertyChanged("Name");
                    }
                    break;

                default:
                    NotifyPropertyChanged(e.PropertyName);
                    break;
            }
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }

        #endregion

        private CMachine Owner { get; set; }

        public string SelectionInfo => Owner.SelectionInfo;

        public void AddState(CMachineState state)
        {
            States.Add(new CMachineStateVM(state));
        }

        public void RemoveState(CMachineState state)
        {
            States.RemoveAt(States.FindIndex(x => x._state == state));
        }

        public ObservableCollection<CMachineStateVM> States { get; }

        #region Commands
        public SimpleCommand cmdCapture { get; private set; }
        public SimpleCommand cmdCaptureMissing { get; private set; }
        public SimpleCommand cmdRestore { get; private set; }
        public SimpleCommand cmdClear { get; private set; }
        public SimpleCommand cmdPurge { get; private set; }
        public SimpleCommand cmdMap { get; private set; }
        public SimpleCommand cmdMapSpecific { get; private set; }
        public SimpleCommand cmdSelectAll { get; private set; }
        public SimpleCommand cmdSelectNone { get; private set; }
        public SimpleCommand cmdSelectStored { get; private set; }
        public SimpleCommand cmdSelectInvert { get; private set; }
        #endregion Commands

        #region Properties
        public CMachineSnapshot ManagerSlotA => Owner.ManagerSlotA;
        public CMachineSnapshot ManagerSlotB => Owner.ManagerSlotB;
        public int ManagerSelA => Owner.ManagerSelA;
        public int ManagerSelB => Owner.ManagerSelB;

        public CMachineSnapshot CurrentSlot => Owner.CurrentSlot;
        public int Slot
        {
            get => Owner.Slot;
            set => Owner.Slot = value;
        }
        public string SlotName
        {
            get => Owner.SlotName;
            set => Owner.SlotName = value;
        }
        public List<CMachineSnapshot> Slots
        {
            get => Owner.Slots;
        }
        public bool ConfirmClear
        {
            get => Owner.ConfirmClear;
            set => Owner.ConfirmClear = value;
        }
        public bool SelectionFollowsSlot
        {
            get => Owner.SelectionFollowsSlot;
            set => Owner.SelectionFollowsSlot = value;
        }
        public bool SelectNewMachines
        {
            get => Owner.SelectNewMachines;
            set => Owner.SelectNewMachines = value;
        }
        public bool CaptureOnSlotChange
        {
            get => Owner.CaptureOnSlotChange;
            set => Owner.CaptureOnSlotChange = value;
        }
        public bool RestoreOnSlotChange
        {
            get => Owner.RestoreOnSlotChange;
            set => Owner.RestoreOnSlotChange = value;
        }
        public bool RestoreOnSongLoad
        {
            get => Owner.RestoreOnSongLoad;
            set => Owner.RestoreOnSongLoad = value;
        }
        public bool RestoreOnStop
        {
            get => Owner.RestoreOnStop;
            set => Owner.RestoreOnStop = value;
        }

        #endregion Properties
    }

    // Machines
    public class CMachineStateVM : CTreeViewItemVM
    {
        public readonly CMachineState _state;

        public CMachineStateVM(CMachineState state)
            : base(null, true)
        {
            _state = state;
            state.VM = this;
            IsChecked = false;
            LoadChildren();
        }

        public string Name => _state.Machine.Name;

        public override bool GotValue => _state.GotValue;

        protected override void LoadChildren()
        {
            if(_state.DataState != null)
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

        internal void OnTrackCountChanged(int newCount, int oldCount)
        {
            int delta = newCount - oldCount;
            if(delta < 0) // track(s?) removed
            {
                // Remove any inactive items from the tree
                foreach(var param in _state.TrackStates.Children)
                {
                    foreach(var track in param.Children.Where(x => x.Active == false))
                    {
                        param.VM.RemoveChild(track.VM);
                        track.VM = null;
                        track.Selected = false;
                    }
                    param.VM.UpdateTreeCheck();
                }
            }
            else if(delta > 0) // track(s?) added
            {
                // Add any properties whose VM is null to the tree
                foreach(var param in _state.TrackStates.Children)
                {
                    foreach (IPropertyState track in param.Children.Where(x => x.Active && x.VM == null))
                    {
                        track.VM = new CPropertyStateVM(track, param.VM);
                        param.VM.AddChild(track.VM);
                    }
                    param.VM.UpdateTreeCheck();
                }
            }
        }
    }

    // Groups
    public class CPropertyStateGroupVM : CTreeViewItemVM
    {
        readonly CPropertyStateGroup _group;

        public CPropertyStateGroupVM(CPropertyStateGroup group, CTreeViewItemVM parentMachine)
            : base(parentMachine, true)
        {
            _group = group;
            group.VM = this;
            IsChecked = false;
            LoadChildren();
        }

        public string Name
        {
            get { return _group.Name; }
        }

        public override bool GotValue => _group.GotValue;

        protected override void LoadChildren()
        {
            foreach (var p in _group.Children)
            {
                Children.Add(new CPropertyStateVM(p, this));
            }
        }
    }

    // Groups
    public class CTrackPropertyStateGroupVM : CTreeViewItemVM
    {
        readonly CTrackPropertyStateGroup _group;

        public CTrackPropertyStateGroupVM(CTrackPropertyStateGroup group, CTreeViewItemVM parent)
            : base(parent, true)
        {
            _group = group;
            group.VM = this;
            IsChecked = false;
            LoadChildren();
        }

        public string Name
        {
            get { return _group.Name; }
        }

        public override bool GotValue => _group.GotValue;

        protected override void LoadChildren()
        {
            foreach (var pg in _group.Children)
            {
                Children.Add(new CPropertyStateGroupVM(pg, this));
            }
        }
    }

    public class CPropertyStateVM : CTreeViewItemVM
    {
        readonly IPropertyState _property;

        public CPropertyStateVM(IPropertyState property, CTreeViewItemVM parent)
            : base(parent, false)
        {
            _property = property;
            property.VM = this;
            IsChecked = _property.Selected;
            _property.SelChanged += OnSelChanged;
            _property.SizeChanged += OnSizeChanged;
        }

        private void OnSizeChanged(object sender, EventArgs e)
        {
            OnPropertyChanged("DisplayName");
        }

        public override bool GotValue => _property.GotValue;            

        protected override void OnCheckChanged()
        {
            _property.Selected = (bool)IsChecked;
            _property.OnSelChanged(new StateChangedEventArgs() { Property = _property, Selected = _property.Selected });
        }

        public string Name => _property.Name;

        public string DisplayName => _property.DisplayName;
    }
}