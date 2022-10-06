using System.Linq;

namespace Snapshot
{
    // Groups
    public class CPropertyStateGroupVM : CTreeViewItemVM
    {
        readonly CPropertyStateGroup _group;

        public CPropertyStateGroupVM(CPropertyStateGroup group, CTreeViewItemVM parentMachine, CSnapshotMachineVM ownerVM)
            : base(parentMachine, true, ownerVM)
        {
            _group = group;
            IsChecked = false;
            LoadChildren();
        }

        public string Name
        {
            get { return _group.Name; }
        }

        public override bool GotValue
        {
            get
            {
                try
                {
                    var c = Children.First(x => x.GotValue == true);
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
                    var c = Children.First(x => x.GotValueA == true);
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
                    var c = Children.First(x => x.GotValueB == true);
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
            foreach (var p in _group.Children)
            {
                Children.Add(new CPropertyStateVM(p, this, _ownerVM));
            }
        }
    }
}