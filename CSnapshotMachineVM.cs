using BuzzGUI.Common;
using System;
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
            
            _showMode = _showModeM = 2; // All

            States = new ObservableCollection<CMachineStateVM>();
            StatesA = new ObservableCollection<CMachineStateVM>();
            StatesB = new ObservableCollection<CMachineStateVM>();
            foreach(CMachineState s in Owner.States)
            {
                AddState(s);
            }

            foreach (CMachineSnapshot s in Owner.Slots)
            {
                s.PropertyChanged += OnSlotPropertyChanged;
            }

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
            CmdClearAll = new SimpleCommand
            {
                CanExecuteDelegate = x => true,
                ExecuteDelegate = x => Owner.ClearAll()
            };
            CmdClearSelected = new SimpleCommand
            {
                CanExecuteDelegate = x => true,
                ExecuteDelegate = x => Owner.ClearSelected()
            };
            CmdClearSelectedAll = new SimpleCommand
            {
                CanExecuteDelegate = x => true,
                ExecuteDelegate = x => Owner.ClearSelectedAll()
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
            CmdFilterClear = new SimpleCommand
            {
                ExecuteDelegate = x => FilterClear()
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
            CmdFilterClearM = new SimpleCommand
            {
                ExecuteDelegate = x => FilterClearM()
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
            CmdActivateA = new SimpleCommand
            {
                ExecuteDelegate = x => { Slot = SlotA.Index; }
            };
            CmdLoadA = new SimpleCommand
            {
                ExecuteDelegate = x => { Slot = SlotA.Index; Owner.ForceRestore(); }
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
            CmdActivateB = new SimpleCommand
            {
                ExecuteDelegate = x => { Slot = SlotB.Index; },
            };
            CmdLoadB = new SimpleCommand
            {
                ExecuteDelegate = x => { Slot = SlotB.Index; Owner.ForceRestore(); },
            };
        }

        private void OnSlotPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch(e.PropertyName)
            {
                case "HasData":
                    NotifyPropertyChanged("Slots");

                    if ((sender as CMachineSnapshot) == CurrentSlot)
                    {
                        NotifyPropertyChanged("States");
                        foreach (CMachineStateVM s in States)
                        {
                            s.OnPropertyChanged("GotValue");
                            s.OnPropertyChanged("DisplayValue");
                        }
                    }

                    if ((sender as CMachineSnapshot) == SlotA)
                    {
                        NotifyPropertyChanged("StatesA");
                        foreach (CMachineStateVM s in StatesA)
                        {
                            s.OnPropertyChanged("GotValue");
                            s.OnPropertyChanged("DisplayValue");
                        }
                    }

                    if ((sender as CMachineSnapshot) == SlotB)
                    {
                        NotifyPropertyChanged("StatesB");
                        NotifyPropertyChanged("StatesB");
                        foreach (CMachineStateVM s in StatesB)
                        {
                            s.OnPropertyChanged("GotValue");
                            s.OnPropertyChanged("DisplayValue");
                        }
                    }

                    NotifyPropertyChanged("SelectionInfo");
                    break;

                default:
                    break;
            }
        }

        private void OnItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "IsExpandedM":
                    break;

                case "IsExpanded":
                    break;

                case "IsChecked":
                    break;

                case "IsCheckedM":
                    break;

                case "GotValue":
                    break;

                case "DisplayValue":
                    break;

                default:
                    break;
            }
        }

        internal string _filterText;
        internal string _filterTextM;
        internal int _showMode;
        internal int _showModeM;

        private void FilterVisibility(ObservableCollection<CMachineStateVM> statelist)
        {
            foreach (CMachineStateVM s in statelist)
            {
                bool show1 = false;
                foreach (CMachinePropertyItemVM c in s.Children) // groups
                {
                    bool show2 = false;
                    foreach (CMachinePropertyItemVM cc in c.Children) // global/attrib/data/trackgroup
                    {
                        //if (cc.Name.Contains(FilterText))
                        if((cc.IsChecked != false) || (cc.Name.IndexOf(FilterText, 0, StringComparison.OrdinalIgnoreCase) != -1))
                        {
                            cc.IsVisible = System.Windows.Visibility.Visible;
                            cc.IsExpanded = true;
                            show1 = show2 = true;
                        }
                        else
                        {
                            cc.IsVisible = System.Windows.Visibility.Collapsed;
                            cc.IsExpanded = false;
                        }
                    }
                    c.IsVisible = show2 ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
                    c.IsExpanded = show2;
                }
                s.IsVisible = show1 ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
                s.IsExpanded = show1;
            }
        }

        private void FilterVisibilityM(ObservableCollection<CMachineStateVM> statelist)
        {
            foreach (CMachineStateVM s in statelist)
            {
                bool show1 = false;
                foreach (CMachinePropertyItemVM c in s.Children) // groups
                {
                    bool show2 = false;
                    foreach (CMachinePropertyItemVM cc in c.Children) // global/attrib/data/trackgroup
                    {
                        //if (cc.Name.Contains(FilterText))
                        if ((cc.IsChecked != false) || (cc.Name.IndexOf(FilterText, 0, StringComparison.OrdinalIgnoreCase) != -1))
                        {
                            cc.IsVisibleM = System.Windows.Visibility.Visible;
                            cc.IsExpandedM = true;
                            show1 = show2 = true;
                        }
                        else
                        {
                            cc.IsVisibleM = System.Windows.Visibility.Collapsed;
                            cc.IsExpandedM = false;
                        }
                    }
                    c.IsVisibleM = show2 ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
                    c.IsExpandedM = show2;
                }
                s.IsVisibleM = show1 ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
                s.IsExpandedM = show1;
            }
        }

        private void UpdateVisibility(ObservableCollection<CMachineStateVM> statelist)
        {
            // Set visibility and expandedness for main treeview
            foreach (CMachineStateVM s in statelist)
            {
                s.IsVisible = GetVisibility(s);
                s.IsExpanded = GetExpanded(s);

                foreach (CMachinePropertyItemVM c in s.Children)
                {
                    c.IsVisible = GetVisibility(c);
                    c.IsExpanded = GetExpanded(c);
                    foreach (CMachinePropertyItemVM cc in c.Children)
                    {
                        cc.IsExpanded = GetExpanded(cc);
                        cc.IsVisible = GetVisibility(cc);
                        foreach (CMachinePropertyItemVM ccc in cc.Children)
                        {
                            GetExpanded(ccc);
                            ccc.IsVisible = GetVisibility(ccc);
                        }
                    }
                }
            }
        }

        private void UpdateVisibilityM(ObservableCollection<CMachineStateVM> statelist)
        {
            // Set visibility and expandedness for main treeview
            foreach (CMachineStateVM s in statelist)
            {
                s.IsVisibleM = GetVisibility(s);
                s.IsExpandedM = GetExpanded(s);

                foreach (CMachinePropertyItemVM c in s.Children)
                {
                    c.IsVisibleM = GetVisibility(c);
                    c.IsExpandedM = GetExpanded(c);
                    foreach (CMachinePropertyItemVM cc in c.Children)
                    {
                        cc.IsExpandedM = GetExpanded(cc);
                        cc.IsVisibleM = GetVisibility(cc);
                        foreach (CMachinePropertyItemVM ccc in cc.Children)
                        {
                            GetExpanded(ccc);
                            ccc.IsVisibleM = GetVisibility(ccc);
                        }
                    }
                }
            }
        }

        public void FilterClear()
        {
            FilterText = "";
        }

        public void FilterClearM()
        {
            FilterTextM = "";
        }

        private void OwnerPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "State":
                    NotifyPropertyChanged("Slots");
                    NotifyPropertyChanged("SlotName");
                    NotifyPropertyChanged("Notes");
                    NotifyPropertyChanged("SelectionInfo");
                    NotifyPropertyChanged("CurrentSlot");
                    foreach (CMachineStateVM s in States)
                    {
                        s.OnPropertyChanged("GotValue");
                        s.OnPropertyChanged("DisplayValue");
                    }
                    NotifyPropertyChanged("SlotA");
                    foreach (CMachineStateVM s in StatesA)
                    {
                        s.OnPropertyChanged("GotValue");
                        s.OnPropertyChanged("DisplayValue");
                    }
                    NotifyPropertyChanged("SlotB");
                    foreach (CMachineStateVM s in StatesB)
                    {
                        s.OnPropertyChanged("GotValue");
                        s.OnPropertyChanged("DisplayValue");
                    }
                    break;

                case "Names":
                    foreach (CMachineStateVM s in States)
                    {
                        s.OnPropertyChanged("Name");
                    }
                    break;

                case "CurrentSlot":
                    {
                        foreach (CMachineStateVM s in States)
                        {
                            s.OnPropertyChanged("GotValue");
                            s.OnPropertyChanged("DisplayValue");
                        }
                    }
                    break;

                case "SlotA":
                    NotifyPropertyChanged("SlotA");
                    NotifyPropertyChanged("CanCopy");
                    foreach (CMachineStateVM s in StatesA)
                    {
                        s.OnPropertyChanged("GotValue");
                        s.OnPropertyChanged("DisplayValue");
                    }
                    break;

                case "SlotB":
                    NotifyPropertyChanged("SlotB");
                    NotifyPropertyChanged("CanCopy");
                    foreach (CMachineStateVM s in StatesB)
                    {
                        s.OnPropertyChanged("GotValue");
                        s.OnPropertyChanged("DisplayValue");
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

        public CMachine Owner { get; private set; }

        public string SelectionInfo => Owner.Info;

        internal System.Windows.Visibility GetVisibility(CMachinePropertyItemVM tvi)
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

        internal bool GetExpanded(CMachinePropertyItemVM tvi)
        {
            return tvi.IsExpanded;
        }

        public void AddState(CMachineState state)
        {
            var s0 = new CMachineStateVM(state, this, 0);
            s0.PropertyChanged += OnItemPropertyChanged;
            States.Add(s0);

            var s1 = new CMachineStateVM(state, this, 1);
            s1.PropertyChanged += OnItemPropertyChanged;
            StatesA.Add(s1);

            var s2 = new CMachineStateVM(state, this, 2);
            s2.PropertyChanged += OnItemPropertyChanged;
            StatesB.Add(s2);
        }

        public void RemoveState(CMachineState state)
        {
            States.RemoveAt(States.FindIndex(x => x._state == state));
            StatesA.RemoveAt(States.FindIndex(x => x._state == state));
            StatesB.RemoveAt(States.FindIndex(x => x._state == state));
        }

        public ObservableCollection<CMachineStateVM> States { get; }
        public ObservableCollection<CMachineStateVM> StatesA { get; }
        public ObservableCollection<CMachineStateVM> StatesB { get; }

        #region Commands
        public SimpleCommand CmdCapture { get; private set; }
        public SimpleCommand CmdCaptureMissing { get; private set; }
        public SimpleCommand CmdRestore { get; private set; }
        public SimpleCommand CmdClear { get; private set; }
        public SimpleCommand CmdClearAll { get; private set; }
        public SimpleCommand CmdClearSelected { get; private set; }
        public SimpleCommand CmdClearSelectedAll { get; private set; }
        public SimpleCommand CmdPurge { get; private set; }
        public SimpleCommand CmdMap { get; private set; }
        public SimpleCommand CmdMapSpecific { get; private set; }
        public SimpleCommand CmdSelectAll { get; private set; }
        public SimpleCommand CmdSelectNone { get; private set; }
        public SimpleCommand CmdSelectStored { get; private set; }
        public SimpleCommand CmdSelectInvert { get; private set; }
        public SimpleCommand CmdFilterClear { get; private set; }

        public SimpleCommand CmdSelectAll_M { get; private set; }
        public SimpleCommand CmdSelectNone_M { get; private set; }
        public SimpleCommand CmdSelectInvert_M { get; private set; }
        public SimpleCommand CmdSelectStoredA { get; private set; }
        public SimpleCommand CmdSelectStoredB { get; private set; }
        public SimpleCommand CmdAtoB { get; private set; }
        public SimpleCommand CmdBtoA { get; private set; }
        public SimpleCommand CmdFilterClearM { get; private set; }

        public SimpleCommand CmdCaptureA { get; private set; }
        public SimpleCommand CmdCaptureMissingA { get; private set; }
        public SimpleCommand CmdPurgeA { get; private set; }
        public SimpleCommand CmdClearSelectedA { get; private set; }
        public SimpleCommand CmdClearA { get; private set; }
        public SimpleCommand CmdRestoreA { get; private set; }
        public SimpleCommand CmdActivateA { get; private set; }
        public SimpleCommand CmdLoadA { get; private set; }

        public SimpleCommand CmdCaptureB { get; private set; }
        public SimpleCommand CmdCaptureMissingB { get; private set; }
        public SimpleCommand CmdPurgeB { get; private set; }
        public SimpleCommand CmdClearSelectedB { get; private set; }
        public SimpleCommand CmdClearB { get; private set; }
        public SimpleCommand CmdRestoreB { get; private set; }
        public SimpleCommand CmdActivateB { get; private set; }
        public SimpleCommand CmdLoadB { get; private set; }

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

        public string FilterText
        {
            get { return _filterText; }
            set
            {
                if (value != _filterText)
                {
                    _filterText = value;
                    NotifyPropertyChanged("FilterText");
                    FilterVisibility(States);
                }
            }
        }

        public string FilterTextM
        {
            get { return _filterTextM; }
            set
            {
                if (value != _filterTextM)
                {
                    _filterTextM = value;
                    NotifyPropertyChanged("FilterTextM");
                    FilterVisibilityM(StatesA);
                    FilterVisibilityM(StatesB);
                }
            }
        }

        public int ShowMode
        {
            get { return _showMode; }
            set
            {
                _showMode = value;
                NotifyPropertyChanged("Filter");
                UpdateVisibility(States);
            }
        }

        public int ShowModeM
        {
            get { return _showModeM; }
            set
            {
                _showModeM = value;
                NotifyPropertyChanged("FilterM");
                UpdateVisibilityM(StatesA);
                UpdateVisibilityM(StatesB);
            }
        }

        public CMachineSnapshot CurrentSlot => Owner.CurrentSlot;

        public string Notes
        {
            get => CurrentSlot.Notes;
            set => CurrentSlot.Notes = value;
        }

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