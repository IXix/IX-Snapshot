using BuzzGUI.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Snapshot
{
    // TreeViewItemVM with extra stuff for dealing with machine properties
    public class CMachinePropertyItemVM : CTreeViewItemVM
    {
        public CMachinePropertyItemVM(CPropertyBase property, CTreeViewItemVM parent, CSnapshotMachineVM ownerVM, int view)
            : base(parent, true)
        {
            _property = property;
            _ownerVM = ownerVM;
            _viewRef = view;
            _childProperties = new HashSet<CPropertyBase>();

            _property.TreeStateChanged += OnTreeStateChanged;
            _property.PropertyStateChanged += OnPropertyStateChanged;

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
            CmdOpenSettings = new SimpleCommand
            {
                ExecuteDelegate = x => { DoSettingsDialog(); }
            };
        }

        private void DoSettingsDialog()
        {
            if (_property is CDataState) return;

            _ownerVM.StoreTempState(this);

            CPropertyDialog dlg = new CPropertyDialog(this);
            dlg.btnCancel.Click += OnPropertyDlg_Cancelled;
            dlg.Show();
        }

        // Put things back if the user cancels
        private void OnPropertyDlg_Cancelled(object sender, System.Windows.RoutedEventArgs e)
        {
            _ownerVM.RestoreTempState(this);
        }

        public int? StoredValue
        {
            get
            {
                return ReferenceSlot().GetPropertyValue(_property);
            }
            set
            {
                if (value != null)
                {
                    ReferenceSlot().SetPropertyValue(_property, (int) value);
                }
            }
        }

        public string StoredValueDescription
        {
            get => ReferenceSlot().GetPropertyValueString(_property);
        }

        public string CurrentValueDescription
        {
            get => _property.CurrentValueString;
        }

        // This signals the UI to update when the state changes from code eg. slot change, capture, restore
        private void OnPropertyStateChanged(object sender, EventArgs e)
        {
            OnPropertyChanged("GotValue");
            OnPropertyChanged("Size");
            OnPropertyChanged("DisplayValue");
            OnPropertyChanged("DisplayName");
            OnPropertyChanged("StoredValue");
            OnPropertyChanged("StoredValueDescription");
        }

        // This signals the UI to update when the tree changes from code eg. the select buttons
        private void OnTreeStateChanged(object sender, TreeStateEventArgs e)
        {
            OnPropertyChanged("IsChecked");
            OnPropertyChanged("IsCheckedM");
            OnPropertyChanged("IsExpanded");
            OnPropertyChanged("IsExpandedM");
        }

        internal readonly CSnapshotMachineVM _ownerVM;
        internal CPropertyBase _property;
        internal HashSet<CPropertyBase> _childProperties;
        protected int _viewRef;

        public SimpleCommand CmdCapture { get; private set; }
        public SimpleCommand CmdRestore { get; private set; }
        public SimpleCommand CmdClear { get; private set; }
        public SimpleCommand CmdClearAll { get; private set; }
        public SimpleCommand CmdCopyAcross { get; private set; }
        public SimpleCommand CmdOpenSettings { get; private set; }

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
            ReferenceSlot().Capture(_childProperties, false);
            _ownerVM.Owner.OnPropertyChanged("State");
        }

        internal void Restore()
        {
            ReferenceSlot().Restore(_childProperties);
        }

        internal void ClearAll()
        {
            foreach(var slot in _ownerVM.Slots)
            {
                slot.Remove(_childProperties);
            }
        }

        internal void Clear()
        {
            ReferenceSlot().Remove(_childProperties);
        }

        internal void CopyAcross(CMachineSnapshot dest)
        {
            dest.CopyFrom(_childProperties, ReferenceSlot());
        }

        public virtual string Name
        {
            get => _property.Name;
        }

        public virtual string DisplayName
        {
            get => _property.DisplayName;
        }

        public virtual bool GotValue
        {
            get => _property.GotValue;
        }

        public virtual string DisplayValue
        {
            get => _property.DisplayValue;
        }

        public virtual int? SmoothingCount
        {
            get => _property.SmoothingCount;
            set => _property.SmoothingCount = value;
        }

        public virtual int? SmoothingUnits
        {
            get => _property.SmoothingUnits;
            set => _property.SmoothingUnits = value;
        }

        public virtual int? SmoothingShape
        {
            get => _property.SmoothingShape;
            set => _property.SmoothingShape = value;
        }

        public bool AllowSmoothing => _property.AllowSmoothing;

        public HashSet<CPropertyBase> Properties => _childProperties;

        public override bool? IsChecked
        {
            get { return _property.Checked; }
            set
            {
                if (value != _property.Checked)
                {
                    if (reentrancyCheck) return;

                    reentrancyCheck = true;

                    _property.Checked = value;

                    if (value != null)
                    {

                        if (Children != null)
                        {
                            foreach (CTreeViewItemVM child in Children)
                            {
                                child.IsChecked = _property.Checked;
                            }
                        }
                    }

                    if (Parent != null)
                    {
                        Parent.UpdateTreeCheck();
                    }

                    OnPropertyChanged("IsChecked");

                    reentrancyCheck = false;
                }
            }
        }

        public override bool? IsCheckedM
        {
            get { return _property.Checked_M; }
            set
            {
                if (value != _property.Checked_M)
                {
                    if (reentrancyCheckM) return;

                    reentrancyCheckM = true;

                    _property.Checked_M = value;

                    if (value != null)
                    {

                        if (Children != null)
                        {
                            foreach (CTreeViewItemVM child in Children)
                            {
                                child.IsCheckedM = _property.Checked_M;
                            }
                        }
                    }

                    if (Parent != null)
                    {
                        Parent.UpdateTreeCheck("M");
                    }

                    OnPropertyChanged("IsCheckedM");

                    reentrancyCheckM = false;
                }
            }
        }

        public override bool IsExpanded // For main UI
        {
            get { return _property.Expanded; }
            set
            {
                if (value != _property.Expanded)
                {
                    _property.Expanded = value;
                    OnPropertyChanged("IsExpanded");
                }

                // Expand all the way up to the root.
                if (_property.Expanded && _property.ParentMachine != null)
                    Parent.IsExpanded = true;
            }
        }

        public override bool IsExpandedM // For main UI
        {
            get { return _property.Expanded_M; }
            set
            {
                if (value != _property.Expanded_M)
                {
                    _property.Expanded_M = value;
                    OnPropertyChanged("IsExpandedM");
                }

                // Expand all the way up to the root.
                if (_property.Expanded_M && _property.ParentMachine != null)
                    Parent.IsExpandedM = true;
            }
        }

        protected override void LoadChildren()
        {
            foreach (CPropertyBase p in _childProperties)
            {
                CMachinePropertyItemVM pvm = new CMachinePropertyItemVM(p, this, _ownerVM, _viewRef);
                Children.Add(pvm);
            }
        }
    }
}
