using BuzzGUI.Common;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace Snapshot
{
        // Main interaction for GUI
        public class CSnapshotMachineVM : INotifyPropertyChanged
    {
        public CSnapshotMachineVM(CMachine owner)
        {
            Owner = owner;

            States = new ObservableCollection<CMachineStateVM>(
                (from state in Owner.States
                 select new CMachineStateVM(state, this))
                .ToList());

            Owner.PropertyChanged += OwnerPropertyChanged;

            CmdCapture = new SimpleCommand
            {
                CanExecuteDelegate = x => true,
                ExecuteDelegate = x => Owner.Capture()
            };
            CmdCaptureMissing = new SimpleCommand
            {
                CanExecuteDelegate = x => true,
                ExecuteDelegate = x => Owner.CaptureMissing()
            };
            CmdRestore = new SimpleCommand
            {
                CanExecuteDelegate = x => true,
                ExecuteDelegate = x => Owner.Restore()
            };
            CmdClear = new SimpleCommand
            {
                CanExecuteDelegate = x => true,
                ExecuteDelegate = x => Owner.Clear()
            };
            CmdPurge = new SimpleCommand
            {
                CanExecuteDelegate = x => true,
                ExecuteDelegate = x => Owner.Purge()
            };
            CmdMap = new SimpleCommand
            {
                ExecuteDelegate = x => Owner.MapCommand(x.ToString(), false)
            };
            CmdMapSpecific = new SimpleCommand
            {
                ExecuteDelegate = x => Owner.MapCommand(x.ToString(), true)
            };
            CmdSelectAll = new SimpleCommand
            {
                ExecuteDelegate = x => Owner.SelectAll()
            };
            CmdSelectNone = new SimpleCommand
            {
                ExecuteDelegate = x => Owner.SelectNone()
            };
            CmdSelectStored = new SimpleCommand
            {
                ExecuteDelegate = x => Owner.SelectStored()
            };
            CmdSelectInvert = new SimpleCommand
            {
                ExecuteDelegate = x => Owner.SelectInvert()
            };

            CmdSelectAll_M = new SimpleCommand
            {
                ExecuteDelegate = x => Owner.SelectAll_M()
            };
            CmdSelectNone_M = new SimpleCommand
            {
                ExecuteDelegate = x => Owner.SelectNone_M()
            };
            CmdSelectInvert_M = new SimpleCommand
            {
                ExecuteDelegate = x => Owner.SelectInvert_M()
            };
            CmdSelectStoredA = new SimpleCommand
            {
                ExecuteDelegate = x => Owner.SelectStoredA()
            };
            CmdSelectStoredB = new SimpleCommand
            {
                ExecuteDelegate = x => Owner.SelectStoredB()
            };
            CmdAtoB = new SimpleCommand
            {
                ExecuteDelegate = x => Owner.CopyAtoB()
            };
            CmdBtoA = new SimpleCommand
            {
                ExecuteDelegate = x => Owner.CopyBtoA()
            };

            CmdCaptureA = new SimpleCommand
            {
                ExecuteDelegate = x => Owner.CaptureA()
            };
            CmdCaptureMissingA = new SimpleCommand
            {
                ExecuteDelegate = x => Owner.CaptureMissingA()
            };
            CmdPurgeA = new SimpleCommand
            {
                ExecuteDelegate = x => Owner.PurgeA()
            };
            CmdClearSelectedA = new SimpleCommand
            {
                ExecuteDelegate = x => Owner.ClearSelectedA()
            };
            CmdClearA = new SimpleCommand
            {
                ExecuteDelegate = x => Owner.ClearA()
            };
            CmdRestoreA = new SimpleCommand
            {
                ExecuteDelegate = x => Owner.RestoreA()
            };

            CmdCaptureB = new SimpleCommand
            {
                ExecuteDelegate = x => Owner.CaptureB()
            };
            CmdCaptureMissingB = new SimpleCommand
            {
                ExecuteDelegate = x => Owner.CaptureMissingB()
            };
            CmdPurgeB = new SimpleCommand
            {
                ExecuteDelegate = x => Owner.PurgeB()
            };
            CmdClearSelectedB = new SimpleCommand
            {
                ExecuteDelegate = x => Owner.ClearSelectedB()
            };
            CmdClearB = new SimpleCommand
            {
                ExecuteDelegate = x => Owner.ClearB()
            };
            CmdRestoreB = new SimpleCommand
            {
                ExecuteDelegate = x => Owner.RestoreB()
            };
        }

        private void UpdateVisibility()
        {
            // Set visibility and expandedness for main treeview
            foreach (CMachineStateVM s in States)
            {
                s.IsVisible = GetVisibility(s);
                s.IsExpanded = GetExpanded(s);

                foreach (CTreeViewItemVM c in s.Children)
                {
                    c.IsVisible = GetVisibility(c);
                    c.IsExpanded = GetExpanded(c);
                    foreach (CTreeViewItemVM cc in c.Children)
                    {
                        cc.IsExpanded = GetExpanded(cc);
                        cc.IsVisible = GetVisibility(cc);
                        foreach (CTreeViewItemVM ccc in cc.Children)
                        {
                            GetExpanded(ccc);
                            ccc.IsVisible = GetVisibility(ccc);
                        }
                    }
                }
            }
        }

        private void UpdateVisibilityM()
        {
            // Set visibility and expandedness for manager treeviews
            foreach (CMachineStateVM s in States)
            {
                s.IsVisibleM = GetVisibilityM(s);
                s.IsExpandedM = GetExpandedM(s);

                foreach (CTreeViewItemVM c in s.Children)
                {
                    c.IsVisibleM = GetVisibility(c);
                    c.IsExpandedM = GetExpanded(c);
                    foreach (CTreeViewItemVM cc in c.Children)
                    {
                        cc.IsExpandedM = GetExpanded(cc);
                        cc.IsVisibleM = GetVisibility(cc);
                        foreach (CTreeViewItemVM ccc in cc.Children)
                        {
                            GetExpanded(ccc);
                            ccc.IsVisibleM = GetVisibility(ccc);
                        }
                    }
                }
            }
        }

        private void OwnerPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "State":
                    NotifyPropertyChanged("CurrentSlot");
                    NotifyPropertyChanged("SlotA");
                    NotifyPropertyChanged("SlotB");
                    NotifyPropertyChanged("SlotName");
                    NotifyPropertyChanged("SelectionInfo");
                    foreach (CMachineStateVM s in States)
                    {
                        s.OnPropertyChanged("GotValue");
                        s.OnPropertyChanged("GotValueA");
                        s.OnPropertyChanged("GotValueB");
                        s.OnPropertyChanged("DisplayValue");
                        s.OnPropertyChanged("DisplayValueA");
                        s.OnPropertyChanged("DisplayValueB");
                    }
                    break;

                case "Names":
                    foreach (CMachineStateVM s in States)
                    {
                        s.OnPropertyChanged("Name");
                    }
                    break;

                case "Filter":
                    UpdateVisibility();
                    break;

                case "FilterM":
                    UpdateVisibilityM();
                    break;

                case "CurrentSlot":
                    {
                        foreach (CMachineStateVM s in States)
                        {
                            s.OnPropertyChanged("GotValueA");
                            s.OnPropertyChanged("DisplayValueA");
                        }
                    }
                    break;

                case "SlotA":
                    NotifyPropertyChanged("SlotA");
                    NotifyPropertyChanged("CanCopy");
                    foreach (CMachineStateVM s in States)
                    {
                        s.OnPropertyChanged("GotValueA");
                        s.OnPropertyChanged("DisplayValueA");
                    }
                    break;

                case "SlotB":
                    NotifyPropertyChanged("SlotB");
                    NotifyPropertyChanged("CanCopy");
                    foreach (CMachineStateVM s in States)
                    {
                        s.OnPropertyChanged("GotValueB");
                        s.OnPropertyChanged("DisplayValueB");
                    }
                    break;

                default:
                    NotifyPropertyChanged(e.PropertyName);
                    break;
            }
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        #endregion

        private CMachine Owner { get; set; }

        public string SelectionInfo => Owner.SelectionInfo;

        internal System.Windows.Visibility GetVisibility(CTreeViewItemVM tvi)
        {
            if (tvi.IsChecked != false)
                return System.Windows.Visibility.Visible; // Don't hide selected items

            switch(ShowMode)
            {
                case 0: // Empty
                    return tvi.GotValue ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;

                case 1: // Stored
                    return tvi.GotValue ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;

                default:
                    return System.Windows.Visibility.Visible;
            }
        }

        internal System.Windows.Visibility GetVisibilityM(CTreeViewItemVM tvi)
        {
            if (tvi.IsCheckedM != false)
                return System.Windows.Visibility.Visible; // Don't hide selected items

            switch (ShowModeM)
            {
                case 0: // Empty
                    return tvi.GotValueM ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;

                case 1: // Stored
                    return tvi.GotValueM ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;

                default:
                    return System.Windows.Visibility.Visible;
            }
        }

        internal bool GetExpanded(CTreeViewItemVM tvi)
        {
            return tvi.IsChecked != false || tvi.GotValue; // Auto-expand selected items, items with selected children and items with values
        }

        internal bool GetExpandedM(CTreeViewItemVM tvi)
        {
            return tvi.IsCheckedM != false || tvi.GotValueM; // Auto-expand selected items, items with selected children and items with values
        }

        public void AddState(CMachineState state)
        {
            States.Add(new CMachineStateVM(state, this));
        }

        public void RemoveState(CMachineState state)
        {
            States.RemoveAt(States.FindIndex(x => x._state == state));
        }

        public ObservableCollection<CMachineStateVM> States { get; }

        #region Commands
        public SimpleCommand CmdCapture { get; private set; }
        public SimpleCommand CmdCaptureMissing { get; private set; }
        public SimpleCommand CmdRestore { get; private set; }
        public SimpleCommand CmdClear { get; private set; }
        public SimpleCommand CmdPurge { get; private set; }
        public SimpleCommand CmdMap { get; private set; }
        public SimpleCommand CmdMapSpecific { get; private set; }
        public SimpleCommand CmdSelectAll { get; private set; }
        public SimpleCommand CmdSelectNone { get; private set; }
        public SimpleCommand CmdSelectStored { get; private set; }
        public SimpleCommand CmdSelectInvert { get; private set; }

        public SimpleCommand CmdSelectAll_M { get; private set; }
        public SimpleCommand CmdSelectNone_M { get; private set; }
        public SimpleCommand CmdSelectInvert_M { get; private set; }
        public SimpleCommand CmdSelectStoredA { get; private set; }
        public SimpleCommand CmdSelectStoredB { get; private set; }
        public SimpleCommand CmdAtoB { get; private set; }
        public SimpleCommand CmdBtoA { get; private set; }

        public SimpleCommand CmdCaptureA { get; private set; }
        public SimpleCommand CmdCaptureMissingA { get; private set; }
        public SimpleCommand CmdPurgeA { get; private set; }
        public SimpleCommand CmdClearSelectedA { get; private set; }
        public SimpleCommand CmdClearA { get; private set; }
        public SimpleCommand CmdRestoreA { get; private set; }

        public SimpleCommand CmdCaptureB { get; private set; }
        public SimpleCommand CmdCaptureMissingB { get; private set; }
        public SimpleCommand CmdPurgeB { get; private set; }
        public SimpleCommand CmdClearSelectedB { get; private set; }
        public SimpleCommand CmdClearB { get; private set; }
        public SimpleCommand CmdRestoreB { get; private set; }
        #endregion Commands

        #region Properties

        public CMachineSnapshot SlotA => Owner.SlotA;

        public CMachineSnapshot SlotB => Owner.SlotB;

        public bool CanCopy => SlotA != SlotB;

        public int SelA
        {
            get { return Owner.SelA; }
            set
            {
                Owner.SelA = value;
            }
        }

        public int SelB
        {
            get { return Owner.SelB; }
            set
            {
                Owner.SelB = value;
            }
        }

        public int ShowMode
        {
            get => Owner.ShowMode;
            set => Owner.ShowMode = value;
        }

        public int ShowModeM
        {
            get => Owner.ShowModeM;
            set => Owner.ShowModeM = value;
        }

        public CMachineSnapshot CurrentSlot => Owner.CurrentSlot;

        public int Slot
        {
            get => Owner.Slot;
            set => Owner.Slot = value;
        }
        public string SlotName
        {
            get => Owner.SlotName;
            set => Owner.SlotName = value;
        }
        public string SlotNameA
        {
            get => Owner.SlotNameA;
            set => Owner.SlotNameA = value;
        }
        public string SlotNameB
        {
            get => Owner.SlotNameB;
            set => Owner.SlotNameB = value;
        }
        public List<CMachineSnapshot> Slots
        {
            get => Owner.Slots;
        }
        public bool ConfirmClear
        {
            get => Owner.ConfirmClear;
            set => Owner.ConfirmClear = value;
        }
        public bool SelectionFollowsSlot
        {
            get => Owner.SelectionFollowsSlot;
            set => Owner.SelectionFollowsSlot = value;
        }
        public bool SelectNewMachines
        {
            get => Owner.SelectNewMachines;
            set => Owner.SelectNewMachines = value;
        }
        public bool CaptureOnSlotChange
        {
            get => Owner.CaptureOnSlotChange;
            set => Owner.CaptureOnSlotChange = value;
        }
        public bool RestoreOnSlotChange
        {
            get => Owner.RestoreOnSlotChange;
            set => Owner.RestoreOnSlotChange = value;
        }
        public bool RestoreOnSongLoad
        {
            get => Owner.RestoreOnSongLoad;
            set => Owner.RestoreOnSongLoad = value;
        }
        public bool RestoreOnStop
        {
            get => Owner.RestoreOnStop;
            set => Owner.RestoreOnStop = value;
        }

        #endregion Properties
    }
}