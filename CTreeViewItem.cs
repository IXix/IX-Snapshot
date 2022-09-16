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
        protected readonly CMachineStateVM _stateVM;

        bool _isExpanded;
        bool _isExpandedM;
        
        bool _isSelected;

        bool? _isChecked;
        bool? _isCheckedA;
        bool? _isCheckedB;
        
        bool _preventManualIndeterminate;
        
        #endregion // Data

        #region Constructors

        protected CTreeViewItemVM(CTreeViewItemVM parent, bool preventManualIndeterminate, CMachineStateVM stateVM)
        {
            _parent = parent;
            _stateVM = stateVM;
            _preventManualIndeterminate = preventManualIndeterminate;
            _isChecked = false;
            _isCheckedA = false;
            _isCheckedB = false;
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

        /// <summary>
        /// If necessary, prevent user click causing indeterminate state.
        /// </summary>
        internal void OnClick()
        {
            if (_preventManualIndeterminate)
            {
                if(_isChecked == null)
                    IsChecked = false; // force this and any children to be unchecked.

                if (_isCheckedA == null)
                    IsCheckedA = false; // force this and any children to be unchecked.

                if (_isCheckedB == null)
                    IsCheckedB = false; // force this and any children to be unchecked.
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

        private bool reentrancyCheckA = false;
        public bool? IsCheckedA
        {
            get { return _isCheckedA; }
            set
            {
                if (value != _isCheckedA)
                {
                    if (reentrancyCheckA) return;

                    reentrancyCheckA = true;

                    _isCheckedA = value;

                    if (value != null)
                    {

                        if (Children != null)
                        {
                            foreach (var child in Children)
                            {
                                child.IsCheckedA = _isCheckedA;
                            }
                        }
                    }

                    if (Parent != null)
                    {
                        Parent.UpdateTreeCheck("A");
                    }

                    OnPropertyChanged("IsCheckedA");

                    OnCheckChanged();

                    reentrancyCheckA = false;
                }
            }
        }

        private bool reentrancyCheckB = false;
        public bool? IsCheckedB
        {
            get { return _isCheckedB; }
            set
            {
                if (value != _isCheckedB)
                {
                    if (reentrancyCheckB) return;

                    reentrancyCheckB = true;

                    _isCheckedB = value;

                    if (value != null)
                    {

                        if (Children != null)
                        {
                            foreach (var child in Children)
                            {
                                child.IsCheckedB = _isCheckedB;
                            }
                        }
                    }

                    if (Parent != null)
                    {
                        Parent.UpdateTreeCheck("B");
                    }

                    OnPropertyChanged("IsCheckedB");

                    OnCheckChanged();

                    reentrancyCheckB = false;
                }
            }
        }


        public virtual bool GotValue
        {
            get => false;
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

                case "A":
                    {
                        var c = Children.Count(x => x.IsCheckedA == true);
                        var i = Children.Count(x => x.IsCheckedA == null);

                        if (c == 0 && i == 0)
                        {
                            IsCheckedA = false;
                        }
                        else if (c == Children.Count)
                        {
                            IsCheckedA = true;
                        }
                        else
                        {
                            IsCheckedA = null;
                        }
                    }
                    break;

                case "B":
                    {
                        var c = Children.Count(x => x.IsCheckedB == true);
                        var i = Children.Count(x => x.IsCheckedB == null);

                        if (c == 0 && i == 0)
                        {
                            IsCheckedB = false;
                        }
                        else if (c == Children.Count)
                        {
                            IsCheckedB = true;
                        }
                        else
                        {
                            IsCheckedB = null;
                        }
                    }
                    break;
            }


        }

        protected void OnSelChanged(object sender, StateChangedEventArgs e)
        {
            IsChecked = e.Checked;
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
            var dummy = 1;
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