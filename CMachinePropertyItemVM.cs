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
            _properties = new List<IPropertyState>();

            CmdCapture = new SimpleCommand
            {
                ExecuteDelegate = x => { Capture(x, _ownerVM.CurrentSlot); }
            };
            CmdRestore = new SimpleCommand
            {
                ExecuteDelegate = x => { Restore(x, _ownerVM.CurrentSlot); },
                CanExecuteDelegate = x => { return GotValue; }
            };
            CmdClear = new SimpleCommand
            {
                ExecuteDelegate = x => { Clear(x, _ownerVM.CurrentSlot); },
                CanExecuteDelegate = x => { return GotValue; }
            };

            CmdCaptureA = new SimpleCommand
            {
                ExecuteDelegate = x => { Capture(x, _ownerVM.SlotA); }
            };
            CmdRestoreA = new SimpleCommand
            {
                ExecuteDelegate = x => { Restore(x, _ownerVM.SlotA); },
                CanExecuteDelegate = x => { return GotValueA; }
            };
            CmdClearA = new SimpleCommand
            {
                ExecuteDelegate = x => { Clear(x, _ownerVM.SlotA); },
                CanExecuteDelegate = x => { return GotValueA; }
            };

            CmdCaptureB = new SimpleCommand
            {
                ExecuteDelegate = x => { Capture(x, _ownerVM.SlotB); }
            };
            CmdRestoreB = new SimpleCommand
            {
                ExecuteDelegate = x => { Restore(x, _ownerVM.SlotB); },
                CanExecuteDelegate = x => { return GotValueB; }
            };
            CmdClearB = new SimpleCommand
            {
                ExecuteDelegate = x => { Clear(x, _ownerVM.SlotB); },
                CanExecuteDelegate = x => { return GotValueB; }
            };
        }

        protected readonly CSnapshotMachineVM _ownerVM;
        protected List<IPropertyState> _properties;

        public SimpleCommand CmdCapture { get; private set; }
        public SimpleCommand CmdRestore { get; private set; }
        public SimpleCommand CmdClear { get; private set; }

        public SimpleCommand CmdCaptureA { get; private set; }
        public SimpleCommand CmdRestoreA { get; private set; }
        public SimpleCommand CmdClearA { get; private set; }

        public SimpleCommand CmdCaptureB { get; private set; }
        public SimpleCommand CmdRestoreB { get; private set; }
        public SimpleCommand CmdClearB { get; private set; }

        internal void Capture(object param, CMachineSnapshot slot)
        {
            slot.Capture(param as List<IPropertyState>, false);

            if(slot == _ownerVM.CurrentSlot)
            {
                OnPropertyChanged("GotValue");
                OnPropertyChanged("DisplayValue");
            }
            if (slot == _ownerVM.SlotA)
            {
                OnPropertyChanged("GotValueA");
                OnPropertyChanged("DisplayValueA");
            }
            if (slot == _ownerVM.SlotB)
            {
                OnPropertyChanged("GotValueB");
                OnPropertyChanged("DisplayValueB");
            }
        }

        internal void Restore(object param, CMachineSnapshot slot)
        {
            _ownerVM.CurrentSlot.Restore(param as List<IPropertyState>);
        }

        internal void Clear(object param, CMachineSnapshot slot)
        {
            slot.Remove(param as List<IPropertyState>);

            if (slot == _ownerVM.CurrentSlot)
            {
                OnPropertyChanged("GotValue");
                OnPropertyChanged("DisplayValue");
            }
            if (slot == _ownerVM.SlotA)
            {
                OnPropertyChanged("GotValueA");
                OnPropertyChanged("DisplayValueA");
            }
            if (slot == _ownerVM.SlotB)
            {
                OnPropertyChanged("GotValueB");
                OnPropertyChanged("DisplayValueB");
            }
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

        public List<IPropertyState> MachineProperties => _properties;
    }
}
