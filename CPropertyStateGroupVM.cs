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
            : base(group, parentMachine, ownerVM, view)
        {
            _group = group;
            _childProperties = group.ChildProperties;

            LoadChildren();
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

        protected override void LoadChildren()
        {
            foreach (CPropertyBase p in _group.ChildProperties)
            {
                CPropertyStateVM s = new CPropertyStateVM(p, this, _ownerVM, _viewRef);
                Children.Add(s);
                s.PropertyChanged += OnChildPropertyChanged;
            }
        }

    }
}