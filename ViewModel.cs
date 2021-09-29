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

        static readonly TreeViewItemViewModel DummyChild = new TreeViewItemViewModel();

        readonly ObservableCollection<TreeViewItemViewModel> _children;
        readonly TreeViewItemViewModel _parent;

        bool _isExpanded;
        bool _isSelected;
        bool? _isChecked;

        #endregion // Data

        #region Constructors

        protected TreeViewItemViewModel(TreeViewItemViewModel parent, bool lazyLoadChildren)
        {
            _parent = parent;

            _children = new ObservableCollection<TreeViewItemViewModel>();

            if (lazyLoadChildren)
                _children.Add(DummyChild);
        }

        // This is used to create the DummyChild instance.
        private TreeViewItemViewModel()
        {
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
        /// Returns true if this object's Children have not yet been populated.
        /// </summary>
        public bool HasDummyChild
        {
            get { return this.Children.Count == 1 && this.Children[0] == DummyChild; }
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
                    this.OnPropertyChanged("IsExpanded");
                }

                // Expand all the way up to the root.
                if (_isExpanded && _parent != null)
                    _parent.IsExpanded = true;

                // Lazy load the child items, if necessary.
                if (this.HasDummyChild)
                {
                    this.Children.Remove(DummyChild);
                    this.LoadChildren();
                }
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
                    this.OnPropertyChanged("IsSelected");
                }
            }
        }

        /// <summary>
        /// Gets/sets whether the TreeViewItem 
        /// associated with this object is checked, unchecked or undetermined.
        /// </summary>
        public bool? IsChecked
        {
            get { return _isChecked; }
            set
            {
                if (value != _isChecked)
                {
                    if(value != null)
                    {
                        _isChecked = value;
                    }
                    else
                    {
                        _isChecked = !_isChecked;
                    }

                    if (Children != null)
                    {
                        foreach (var child in Children)
                        {
                            child.IsChecked = (bool)_isChecked;
                        }
                    }

                    this.OnPropertyChanged("IsChecked");
                }
            }
        }

        /// <summary>
        /// Invoked when the child items need to be loaded on demand.
        /// Subclasses can override this to populate the Children collection.
        /// </summary>
        protected virtual void LoadChildren()
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
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion // INotifyPropertyChanged Members
    }

    // Main interaction for GUI
    public class SnapshotVM
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

        Machine Owner { get; set; }

        public void AddState(MachineState state)
        {
            States.Add(new MachineStateVM(state));
        }

        public void RemoveState(MachineState state)
        {
            States.RemoveAt(States.FindIndex(x => x._state == state));
        }

        internal void OnMachineCheckboxClick(object sender, RoutedEventArgs e)
        {
            // If checked, select all children
            // If unchecked deselect all children
            var chk = sender as CheckBox;
            var item = chk.DataContext as TreeViewItemViewModel;
            foreach(var child in item.Children)
            {
                child.IsSelected = item.IsSelected;
            }
        }

        internal void OnGroupCheckboxClick(object sender, RoutedEventArgs e)
        {
            // FIXME
            // Set to checked/unchecked if null, user can't set to indeterminate state
            // Work up tree getting parents to check what check state they should have
            throw new NotImplementedException();
        }

        internal void OnParameterCheckboxClick(object sender, RoutedEventArgs e)
        {
            // FIXME
            // Set to checked/unchecked if null, user can't set to indeterminate state
            // Work up tree getting parents to check what check state they should have
            throw new NotImplementedException();
        }

        internal void OnAttributeCheckboxClick(object sender, RoutedEventArgs e)
        {
            // FIXME
            // Set to checked/unchecked if null, user can't set to indeterminate state
            // Work up tree getting parents to check what check state they should have
            throw new NotImplementedException();
        }

        public ObservableCollection<MachineStateVM> States { get; }

        #region Commands
        public SimpleCommand cmdCapture { get; private set; }
        public SimpleCommand cmdCaptureMissing { get; private set; }
        public SimpleCommand cmdRestore { get; private set; }
        public SimpleCommand cmdClear { get; private set; }
        public SimpleCommand cmdPurge { get; private set; }
        public SimpleCommand cmdTest { get; private set; }
        #endregion Commands
    }

    // Machines
    public class MachineStateVM : TreeViewItemViewModel
    {
        public readonly MachineState _state;

        public MachineStateVM(MachineState state)
            : base(null, false)
        {
            _state = state;
            LoadChildren();
        }

        public string MachineName
        {
            get { return _state.Machine.Name; }
        }

        protected override void LoadChildren()
        {
            foreach (IParameterGroup group in _state.Machine.ParameterGroups)
                base.Children.Add(new MachinePropertyGroupVM(group, this));
        }
    }

    // Groups
    public class MachinePropertyGroupVM : TreeViewItemViewModel
    {
        readonly IParameterGroup _group;

        public MachinePropertyGroupVM(IParameterGroup group, MachineStateVM parentMachine)
            : base(parentMachine, false)
        {
            _group = group;
            LoadChildren();
        }

        public ParameterGroupType Type
        {
            get { return _group.Type; }
        }

        protected override void LoadChildren()
        {
            foreach (IParameter param in _group.Parameters)
                base.Children.Add(new MachineParameterVM(param, this));
        }
    }

    // Param
    public class MachineParameterVM : TreeViewItemViewModel
    {
        readonly IParameter _param;

        public MachineParameterVM(IParameter param, MachinePropertyGroupVM parentGroup)
            : base(parentGroup, false)
        {
            _param = param;
        }

        public string Name
        {
            get { return _param.Name; }
        }
    }

    // Attib
    public class MachineAttributeVM : TreeViewItemViewModel
    {
        readonly IAttribute _attrib;

        public MachineAttributeVM(IAttribute attrib, MachinePropertyGroupVM parentGroup)
            : base(parentGroup, false)
        {
            _attrib = attrib;
        }

        public string Name
        {
            get { return _attrib.Name; }
        }
    }
}