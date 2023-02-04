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

        bool _isExpanded;
        bool _isExpandedM;
        
        bool _isSelected;
        bool _isSelectedM;

        System.Windows.Visibility _isVisible;
        System.Windows.Visibility _isVisibleM;

        bool? _isChecked;
        bool? _isCheckedM;
        readonly bool _preventManualIndeterminate;
        
        #endregion // Data

        #region Constructors

        protected CTreeViewItemVM(CTreeViewItemVM parent, bool preventManualIndeterminate)
        {
            _parent = parent;
            _preventManualIndeterminate = preventManualIndeterminate;
            _isChecked = false;
            _isCheckedM = false;
            _isSelected = false;
            _isSelectedM = false;
            _isVisible = System.Windows.Visibility.Visible;
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

        public System.Windows.Visibility IsVisible
        {
            get { return _isVisible; }
            set
            {
                if (value != _isVisible)
                {
                    _isVisible = value;
                    OnPropertyChanged("IsVisible");
                }
            }
        }

        public System.Windows.Visibility IsVisibleM
        {
            get { return _isVisibleM; }
            set
            {
                if (value != _isVisibleM)
                {
                    _isVisibleM = value;
                    OnPropertyChanged("IsVisibleM");
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
                if(IsChecked == null)
                    IsChecked = false; // force this and any children to be unchecked.

                if (IsCheckedM == null)
                    IsCheckedM = false; // force this and any children to be unchecked.
            }
        }

        /// <summary>
        /// Gets/sets whether the TreeViewItem 
        /// associated with this object is checked, unchecked or undetermined.
        /// </summary>
        protected bool reentrancyCheck = false;
        public virtual bool? IsChecked
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
                            foreach (CTreeViewItemVM child in Children)
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

        protected bool reentrancyCheckM = false;
        public virtual bool? IsCheckedM
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
                            foreach (CTreeViewItemVM child in Children)
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

        internal void UpdateTreeCheck(string tree = "")
        {
            if (Children.Count > 0)
            {
                switch (tree)
                {
                    case "":
                        {
                            int c = Children.Count(x => x.IsChecked == true);
                            int i = Children.Count(x => x.IsChecked == null);

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
                            int c = Children.Count(x => x.IsCheckedM == true);
                            int i = Children.Count(x => x.IsCheckedM == null);

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
        }

        protected void CheckChanged(object sender, TreeStateEventArgs e)
        {
            IsChecked = e.Checked;
            IsCheckedM = e.Checked_M;
        }

        /// <summary>
        /// Invoked when the child items need to be loaded on demand.
        /// Subclasses can override this to populate the Children collection.
        /// </summary>
        protected virtual void LoadChildren()
        {
            throw new System.NotImplementedException();
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
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        internal bool RemoveChild(CTreeViewItemVM VM)
        {
            if (Children.Remove(VM))
                return true;

            foreach (CTreeViewItemVM c in Children)
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