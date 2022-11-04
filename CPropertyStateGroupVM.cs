using System.Linq;

namespace Snapshot
{
    // Groups
    public class CPropertyStateGroupVM : CMachinePropertyItemVM
    {
        readonly CPropertyStateGroup _group;

        public CPropertyStateGroupVM(CPropertyStateGroup group, CTreeViewItemVM parentMachine, CSnapshotMachineVM ownerVM)
            : base(parentMachine, true, ownerVM)
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
                    _ = Children.First(x => (x as CMachinePropertyItemVM).GotValue == true);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        public override bool GotValueA
        {
            get
            {
                try
                {
                    _ = Children.First(x => (x as CMachinePropertyItemVM).GotValueA == true);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        public override bool GotValueB
        {
            get
            {
                try
                {
                    _ = Children.First(x => (x as CMachinePropertyItemVM).GotValueB == true);
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
            foreach (IPropertyState p in _group.Children)
            {
                Children.Add(new CPropertyStateVM(p, this, _ownerVM));
            }
        }
    }
}