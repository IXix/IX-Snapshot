using BuzzGUI.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Snapshot
{
    // TreeViewItemVM with extra stuff for dealing with machine properties
    public class CMachinePropertyItemVM : CTreeViewItemVM
    {
        protected CMachinePropertyItemVM(CTreeViewItemVM parent, bool preventManualIndeterminate, CSnapshotMachineVM ownerVM)
            : base(parent, preventManualIndeterminate)

        {
            _ownerVM = ownerVM;

            CmdCapture = new SimpleCommand
            {
                ExecuteDelegate = x => { Capture(x); }
            };
            CmdRestore = new SimpleCommand
            {
                ExecuteDelegate = x => { Restore(x); },
                CanExecuteDelegate = x => { return GotValue; }
            };
            CmdClear = new SimpleCommand
            {
                ExecuteDelegate = x => { Clear(x); },
                CanExecuteDelegate = x => { return GotValue; }
            };
        }

        protected readonly CSnapshotMachineVM _ownerVM;

        public SimpleCommand CmdCapture { get; private set; }
        public SimpleCommand CmdRestore { get; private set; }
        public SimpleCommand CmdClear { get; private set; }

        internal void Capture(object param)
        {
            CPropertyBase p = param as CPropertyBase;
            _ownerVM.CurrentSlot.Capture(p);
            OnPropertyChanged("GotValue");
            OnPropertyChanged("DisplayValue");
        }

        internal void Restore(object param)
        {
            CPropertyBase p = param as CPropertyBase;
            _ownerVM.CurrentSlot.Restore(p);
        }

        internal void Clear(object param)
        {
            CPropertyBase p = param as CPropertyBase;
            _ownerVM.CurrentSlot.Remove(p);
            OnPropertyChanged("GotValue");
            OnPropertyChanged("DisplayValue");
        }

        public virtual string Name
        {
            get => "";
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

        public virtual bool GotValueM
        {
            get => GotValueA || GotValueB;
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
    }
}
