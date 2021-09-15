using BuzzGUI.Common;
using BuzzGUI.Interfaces;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

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

        #region Children

        /// <summary>
        /// Returns the logical child items of this object.
        /// </summary>
        public ObservableCollection<TreeViewItemViewModel> Children
        {
            get { return _children; }
        }

        #endregion // Children

        #region HasLoadedChildren

        /// <summary>
        /// Returns true if this object's Children have not yet been populated.
        /// </summary>
        public bool HasDummyChild
        {
            get { return this.Children.Count == 1 && this.Children[0] == DummyChild; }
        }

        #endregion // HasLoadedChildren

        #region IsExpanded

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

        #endregion // IsExpanded

        #region IsSelected

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

        #endregion // IsSelected

        #region LoadChildren

        /// <summary>
        /// Invoked when the child items need to be loaded on demand.
        /// Subclasses can override this to populate the Children collection.
        /// </summary>
        protected virtual void LoadChildren()
        {
        }

        #endregion // LoadChildren

        #region Parent

        public TreeViewItemViewModel Parent
        {
            get { return _parent; }
        }

        #endregion // Parent

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

    // Expose collection of machines
    public class SnapshotVM
    {
        public SnapshotVM(Collection<MachineState> states)
        {
            States = new ObservableCollection<MachineStateVM>(
                (from state in states
                 select new MachineStateVM(state))
                .ToList());
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
    }

    // Machines
    public class MachineStateVM : TreeViewItemViewModel
    {
        public readonly MachineState _state;

        public MachineStateVM(MachineState state)
            : base(null, true)
        {
            _state = state;
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
            : base(parentMachine, true)
        {
            _group = group;
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

    // Param/Attib
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
}