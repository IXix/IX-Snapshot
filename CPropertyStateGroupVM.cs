using System;
using System.ComponentModel;
using System.Linq;

namespace Snapshot
{
    // Groups
    public class CPropertyStateGroupVM : CMachinePropertyItemVM
    {
        readonly CPropertyStateGroup _group;

        public CPropertyStateGroupVM(CPropertyStateGroup group, CTreeViewItemVM parentMachine, CSnapshotMachineVM ownerVM, int view)
            : base(parentMachine, true, ownerVM, view)
        {
            _group = group;
            _properties = group.Children;

            LoadChildren();
        }

        public override string Name
        {
            get { return _group.Name; }
        }

        public override bool GotValue
        {
            get
            {
                try
                {
                    _ = Children.First(x => (x as CMachinePropertyItemVM).GotValue);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        public override int? SmoothingCount => _group.SmoothingCount;

        public override int? SmoothingUnits => _group.SmoothingUnits;

        private void OnChildPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            NotifyStateChanged();
        }

        protected override void LoadChildren()
        {
            foreach (IPropertyState p in _group.Children)
            {
                CPropertyStateVM s = new CPropertyStateVM(p, this, _ownerVM, _viewRef);
                s.PropertyChanged += OnChildPropertyChanged;
                Children.Add(s);
            }
        }

    }
}