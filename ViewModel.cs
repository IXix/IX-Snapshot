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
    public class TreeViewItemViewModel : INotifyPropertyChanged
    {
        #region Data

        readonly ObservableCollection<TreeViewItemViewModel> _children;
        readonly TreeViewItemViewModel _parent;

        bool _isExpanded;
        bool _isSelected;
        bool? _isChecked;
        bool _preventManualIndeterminate;

        #endregion // Data

        #region Constructors

        protected TreeViewItemViewModel(TreeViewItemViewModel parent, bool preventManualIndeterminate)
        {
            _parent = parent;
            _preventManualIndeterminate = preventManualIndeterminate;
            _children = new ObservableCollection<TreeViewItemViewModel>();
        }

        #endregion // Constructors

        #region Presentation Members

        /// <summary>
        /// Returns the logical child items of this object.
        /// </summary>
        public ObservableCollection<TreeViewItemViewModel> Children
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

        void UpdateTreeCheck()
        {
            var c = Children.Count(x => x.IsChecked == true);
            var i = Children.Count(x => x.IsChecked == null);

            if(c == 0 && i == 0)
            {
                IsChecked = false;
                return;
            }

            if (c == Children.Count)
            {
                IsChecked = true;
                return;
            }

            IsChecked = null;
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

        public TreeViewItemViewModel Parent
        {
            get { return _parent; }
        }

        #endregion // Presentation Members

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion // INotifyPropertyChanged Members
    }

    // Main interaction for GUI
    public class SnapshotVM : INotifyPropertyChanged
    {
        public SnapshotVM(Machine owner)
        {
            Owner = owner;
            States = new ObservableCollection<MachineStateVM>(
                (from state in Owner.States
                 select new MachineStateVM(state))
                .ToList());

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

        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }

        #endregion

        Machine Owner { get; set; }

        private string selectionInfoText;
        public string SelectionInfo
        {
            get { return selectionInfoText; }
            set
            {
                selectionInfoText = value;
                NotifyPropertyChanged("SelectionInfo");
            }
        }

        public void AddState(MachineState state)
        {
            States.Add(new MachineStateVM(state));
        }

        public void RemoveState(MachineState state)
        {
            States.RemoveAt(States.FindIndex(x => x._state == state));
        }

        public ObservableCollection<MachineStateVM> States { get; }

        #region Commands
        public SimpleCommand cmdCapture { get; private set; }
        public SimpleCommand cmdCaptureMissing { get; private set; }
        public SimpleCommand cmdRestore { get; private set; }
        public SimpleCommand cmdClear { get; private set; }
        public SimpleCommand cmdPurge { get; private set; }
        #endregion Commands

        #region Properties

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
            get => Owner.RestoreOnSlotChange;
            set => Owner.RestoreOnSlotChange = value;
        }
        public bool RestoreOnStop
        {
            get => Owner.RestoreOnStop;
            set => Owner.RestoreOnStop = value;
        }

        #endregion Properties
    }

    // Machines
    public class MachineStateVM : TreeViewItemViewModel
    {
        public readonly MachineState _state;

        public MachineStateVM(MachineState state)
            : base(null, true)
        {
            _state = state;
            IsChecked = false;
            LoadChildren();
        }

        public string MachineName
        {
            get { return _state.Machine.Name; }
        }

        protected override void LoadChildren()
        {
            if(_state.DataStates != null)
                Children.Add(new PropertyStateVM(_state.DataStates, this));

            if (_state.AttributeStates.Children.Count > 0)
                Children.Add(new PropertyStateGroupVM(_state.AttributeStates, this));

            if (_state.GlobalStates.Children.Count > 0)
                Children.Add(new PropertyStateGroupVM(_state.GlobalStates, this));

            if (_state.TrackStates.Children.Count > 0)
                Children.Add(new TrackPropertyStateGroupVM(_state.TrackStates, this));
        }
    }

    // Groups
    public class PropertyStateGroupVM : TreeViewItemViewModel
    {
        readonly PropertyStateGroup _group;

        public PropertyStateGroupVM(PropertyStateGroup group, TreeViewItemViewModel parentMachine)
            : base(parentMachine, true)
        {
            _group = group;
            IsChecked = false;
            LoadChildren();
        }

        public string Name
        {
            get { return _group.Name; }
        }

        protected override void LoadChildren()
        {
            foreach (var p in _group.Children)
                Children.Add(new PropertyStateVM(p, this));
        }
    }

    // Groups
    public class TrackPropertyStateGroupVM : TreeViewItemViewModel
    {
        readonly TrackPropertyStateGroup _group;

        public TrackPropertyStateGroupVM(TrackPropertyStateGroup group, TreeViewItemViewModel parent)
            : base(parent, true)
        {
            _group = group;
            IsChecked = false;
            LoadChildren();
        }

        public string Name
        {
            get { return _group.Name; }
        }

        protected override void LoadChildren()
        {
            foreach (var pg in _group.Children)
                Children.Add(new PropertyStateGroupVM(pg, this));
        }
    }

    public class PropertyStateVM : TreeViewItemViewModel
    {
        readonly IPropertyState _property;

        public PropertyStateVM(IPropertyState property, TreeViewItemViewModel parent)
            : base(parent, false)
        {
            _property = property;
            IsChecked = _property.Selected;
            _property.ValChanged += OnValChanged;
        }

        private void OnValChanged(object sender, StateChangedEventArgs e)
        {
            OnPropertyChanged("Name");
        }

        protected override void OnCheckChanged()
        {
            _property.Selected = (bool)IsChecked;
            _property.OnSelChanged(new StateChangedEventArgs() { Property = _property, Selected = _property.Selected });
        }

        public string Name => _property.Name;
    }
}