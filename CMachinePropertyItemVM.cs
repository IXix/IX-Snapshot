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
        protected CMachinePropertyItemVM(CTreeViewItemVM parent, bool preventManualIndeterminate, CSnapshotMachineVM ownerVM, int view
            )
            : base(parent, preventManualIndeterminate)

        {
            _ownerVM = ownerVM;
            _viewRef = view;
            _properties = new HashSet<IPropertyState>();

            Init();

            CmdCapture = new SimpleCommand
            {
                ExecuteDelegate = x => { Capture(); }
            };
            CmdRestore = new SimpleCommand
            {
                ExecuteDelegate = x => { Restore(); },
                CanExecuteDelegate = x => { return GotValue; }
            };
            CmdClear = new SimpleCommand
            {
                ExecuteDelegate = x => { Clear(); },
                CanExecuteDelegate = x => { return GotValue; }
            };
            CmdClearAll = new SimpleCommand
            {
                ExecuteDelegate = x =>
                {
                    string msg = string.Format("Remove {0} from all slots? Are you sure?", Name);
                    if (_ownerVM.Owner.Confirm("Confirm clear", msg, true)) ClearAll();
                },
            };

        }

        protected readonly CSnapshotMachineVM _ownerVM;
        protected HashSet<IPropertyState> _properties;
        protected int _viewRef;

        public SimpleCommand CmdCapture { get; private set; }
        public SimpleCommand CmdRestore { get; private set; }
        public SimpleCommand CmdClear { get; private set; }
        public SimpleCommand CmdClearAll { get; private set; }
        public SimpleCommand CmdCopyAcross { get; private set; }

        internal Func<CMachineSnapshot> ReferenceSlot;

        private void Init()
        {
            switch (_viewRef)
            {
                case 0: // "MachineList"
                    ReferenceSlot = () => { return _ownerVM.CurrentSlot; };
                    CmdCopyAcross = new SimpleCommand
                    {
                        ExecuteDelegate = x => { /*do nothing */ }
                    };
                    break;

                case 1: // "ListA"
                    ReferenceSlot = () => { return _ownerVM.SlotA; };
                    CmdCopyAcross = new SimpleCommand
                    {
                        ExecuteDelegate = x => {CopyAcross(_ownerVM.SlotB);}
                    };
                    break;

                case 2: // "ListB"
                    ReferenceSlot = () => { return _ownerVM.SlotB; };
                    CmdCopyAcross = new SimpleCommand
                    {
                        ExecuteDelegate = x => { CopyAcross(_ownerVM.SlotA); }
                    };
                    break;

                default:
                    ReferenceSlot = null;
                    break;
            }
        }

        internal void Capture()
        {
            ReferenceSlot().Capture(_properties, false);
            OnPropertyChanged("GotValue");
            OnPropertyChanged("DisplayValue");
        }

        internal void Restore()
        {
            ReferenceSlot().Restore(_properties);
        }

        internal void ClearAll()
        {
            foreach(var slot in _ownerVM.Slots)
            {
                slot.Remove(_properties);
            }
            OnPropertyChanged("GotValue");
            OnPropertyChanged("DisplayValue");
        }

        internal void Clear()
        {
            ReferenceSlot().Remove(_properties);
            OnPropertyChanged("GotValue");
            OnPropertyChanged("DisplayValue");
        }

        internal void CopyAcross(CMachineSnapshot dest)
        {
            dest.CopyFrom(_properties, ReferenceSlot());
            OnPropertyChanged("GotValue");
            OnPropertyChanged("DisplayValue");
        }

        public virtual string Name
        {
            get => "";
        }

        public virtual string DisplayName
        {
            get => Name;
        }

        public virtual bool GotValue
        {
            get => false;
        }

        public virtual string DisplayValue
        {
            get => "";
        }

        public virtual int? SmoothingCount
        {
            get => null;
        }

        public virtual int? SmoothingUnits
        {
            get => null;
        }

        public HashSet<IPropertyState> Properties => _properties;

        protected event EventHandler<StateChangedEventArgs> StateChanged;
        protected void NotifyStateChanged()
        {
            StateChangedEventArgs e = new StateChangedEventArgs()
            {
                Checked = IsChecked,
                Checked_M = IsCheckedM,
                Expanded = IsExpanded,
                Expanded_M = IsExpandedM,
            };
            StateChanged?.Invoke(this, e);
        }
    }
}
