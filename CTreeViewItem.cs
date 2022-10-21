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
    public class CTreeViewItemVM : INotifyPropertyChanged
    {
        #region Data

        readonly ObservableCollection<CTreeViewItemVM> _children;
        readonly CTreeViewItemVM _parent;
        protected readonly CSnapshotMachineVM _ownerVM;

        bool _isExpanded;
        bool _isExpandedM;
        
        bool _isSelected;
        bool _isSelectedM;

        bool? _isChecked;
        bool? _isCheckedM;
        
        bool _preventManualIndeterminate;
        
        #endregion // Data

        #region Constructors

        protected CTreeViewItemVM(CTreeViewItemVM parent, bool preventManualIndeterminate, CSnapshotMachineVM ownerVM)
        {
            _parent = parent;
            _ownerVM = ownerVM;
            _preventManualIndeterminate = preventManualIndeterminate;
            _isChecked = false;
            _isCheckedM = false;
            _isSelected = false;
            _isSelectedM = false;
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
        public bool IsExpanded // For main UI
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

        public bool IsExpandedM // For both manager panes
        {
            get { return _isExpandedM; }
            set
            {
                if (value != _isExpandedM)
                {
                    _isExpandedM = value;
                    OnPropertyChanged("IsExpandedM");
                }

                // Expand all the way up to the root.
                if (_isExpandedM && _parent != null)
                    _parent.IsExpandedM = true;
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

        public bool IsSelectedM
        {
            get { return _isSelectedM; }
            set
            {
                if (value != _isSelectedM)
                {
                    _isSelectedM = value;
                    OnPropertyChanged("IsSelectedM");
                }
            }
        }

        /// <summary>
        /// If necessary, prevent user click causing indeterminate state.
        /// </summary>
        internal void OnClick()
        {
            if (_preventManualIndeterminate)
            {
                if(_isChecked == null)
                    IsChecked = false; // force this and any children to be unchecked.

                if (_isCheckedM == null)
                    IsCheckedM = false; // force this and any children to be unchecked.
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

                    if (value != null)
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

        private bool reentrancyCheckM = false;
        public bool? IsCheckedM
        {
            get { return _isCheckedM; }
            set
            {
                if (value != _isCheckedM)
                {
                    if (reentrancyCheckM) return;

                    reentrancyCheckM = true;

                    _isCheckedM = value;

                    if (value != null)
                    {

                        if (Children != null)
                        {
                            foreach (var child in Children)
                            {
                                child.IsCheckedM = _isCheckedM;
                            }
                        }
                    }

                    if (Parent != null)
                    {
                        Parent.UpdateTreeCheck("M");
                    }

                    OnPropertyChanged("IsCheckedM");

                    OnCheckChanged();

                    reentrancyCheckM = false;
                }
            }
        }

        public virtual bool GotValue
        {
            get => false;
        }

        public virtual bool GotValueA
        {
            get => false;
        }

        public virtual bool GotValueB
        {
            get => false;
        }

        public virtual string DisplayValue
        {
            get => "";
        }

        public virtual string DisplayValueA
        {
            get => "";
        }

        public virtual string DisplayValueB
        {
            get => "";
        }

        internal void UpdateTreeCheck(string tree = "")
        {
            switch(tree)
            {
                case "":
                    {
                        var c = Children.Count(x => x.IsChecked == true);
                        var i = Children.Count(x => x.IsChecked == null);

                        if (c == 0 && i == 0)
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
                    break;

                case "M":
                    {
                        var c = Children.Count(x => x.IsCheckedM == true);
                        var i = Children.Count(x => x.IsCheckedM == null);

                        if (c == 0 && i == 0)
                        {
                            IsCheckedM = false;
                        }
                        else if (c == Children.Count)
                        {
                            IsCheckedM = true;
                        }
                        else
                        {
                            IsCheckedM = null;
                        }
                    }
                    break;
            }
        }

        protected void OnSelChanged(object sender, StateChangedEventArgs e)
        {
            IsChecked = e.Checked;
            //IsCheckedM = e.Checked_M;
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
                foreach (var c in Children)
                {
                    c.OnPropertyChanged(propertyName);
                }
            }
        }

        internal bool RemoveChild(CTreeViewItemVM VM)
        {
            if (Children.Remove(VM))
                return true;

            foreach (var c in Children)
            {
                if (c.RemoveChild(VM))
                    return true;
            }

            return false;
        }

        internal void AddChild(CTreeViewItemVM VM)
        {
            if (!Children.Contains(VM))
            {
                Children.Add(VM);
            }
        }

        #endregion // INotifyPropertyChanged Members
    }
}