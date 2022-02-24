namespace Snapshot
{
    // Groups
    public class CPropertyStateGroupVM : CTreeViewItemVM
    {
        readonly CPropertyStateGroup _group;

        public CPropertyStateGroupVM(CPropertyStateGroup group, CTreeViewItemVM parentMachine)
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

        public override bool GotValue => _group.GotValue;

        protected override void LoadChildren()
        {
            foreach (var p in _group.Children)
            {
                Children.Add(new CPropertyStateVM(p, this));
            }
        }
    }
}