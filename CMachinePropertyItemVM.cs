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
            _properties = new HashSet<IPropertyState>();

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
            CmdClearAll = new SimpleCommand
            {
                ExecuteDelegate = x => {
                    string msg = string.Format("Remove {0} from all slots? Are you sure?", Name);
                    if (_ownerVM.Owner.Confirm("Confirm clear", msg, true)) ClearAll(x);
                },
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

            CmdCopyAtoB = new SimpleCommand
            {
                ExecuteDelegate = x => { Copy(x, _ownerVM.SlotA, _ownerVM.SlotB); },
            };
            CmdCopyBtoA = new SimpleCommand
            {
                ExecuteDelegate = x => { Copy(x, _ownerVM.SlotB, _ownerVM.SlotA); },
            };
        }

        protected readonly CSnapshotMachineVM _ownerVM;
        protected HashSet<IPropertyState> _properties;

        public SimpleCommand CmdCapture { get; private set; }
        public SimpleCommand CmdRestore { get; private set; }
        public SimpleCommand CmdClear { get; private set; }
        public SimpleCommand CmdClearAll { get; private set; }

        public SimpleCommand CmdCaptureA { get; private set; }
        public SimpleCommand CmdRestoreA { get; private set; }
        public SimpleCommand CmdClearA { get; private set; }

        public SimpleCommand CmdCaptureB { get; private set; }
        public SimpleCommand CmdRestoreB { get; private set; }
        public SimpleCommand CmdClearB { get; private set; }

        public SimpleCommand CmdCopyAtoB { get; private set; }
        public SimpleCommand CmdCopyBtoA { get; private set; }

        internal void Capture(object param, CMachineSnapshot slot)
        {
            slot.Capture(param as HashSet<IPropertyState>, false);

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
            slot.Restore(param as HashSet<IPropertyState>);
        }

        internal void ClearAll(object param)
        {
            foreach(var slot in _ownerVM.Slots)
            {
                Clear(param, slot);
            }
        }

        internal void Clear(object param, CMachineSnapshot slot)
        {
            slot.Remove(param as HashSet<IPropertyState>);

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

        internal void Copy(object param, CMachineSnapshot src, CMachineSnapshot dest)
        {
            dest.CopyFrom(param as HashSet<IPropertyState>, src);

            if (dest == _ownerVM.SlotA)
            {
                OnPropertyChanged("GotValueA");
                OnPropertyChanged("DisplayValueA");
            }
            if (dest == _ownerVM.SlotB)
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

        public HashSet<IPropertyState> MachineProperties => _properties;
    }
}
