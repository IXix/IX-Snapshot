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

            cmdCapture = new SimpleCommand
            {
                CanExecuteDelegate = x => true,
                ExecuteDelegate = x => Owner.Capture()
            };
            cmdCaptureMissing = new SimpleCommand
            {
                CanExecuteDelegate = x => true,
                ExecuteDelegate = x => Owner.CaptureMissing()
            };
            cmdRestore = new SimpleCommand
            {
                CanExecuteDelegate = x => true,
                ExecuteDelegate = x => Owner.Restore()
            };
            cmdClear = new SimpleCommand
            {
                CanExecuteDelegate = x => true,
                ExecuteDelegate = x => Owner.Clear()
            };
            cmdPurge = new SimpleCommand
            {
                CanExecuteDelegate = x => true,
                ExecuteDelegate = x => Owner.Purge()
            };
            cmdMap = new SimpleCommand
            {
                ExecuteDelegate = x => Owner.MapCommand(x.ToString(), false)
            };
            cmdMapSpecific = new SimpleCommand
            {
                ExecuteDelegate = x => Owner.MapCommand(x.ToString(), true)
            };
            cmdSelectAll = new SimpleCommand
            {
                ExecuteDelegate = x => Owner.SelectAll()
            };
            cmdSelectNone = new SimpleCommand
            {
                ExecuteDelegate = x => Owner.SelectNone()
            };
            cmdSelectStored = new SimpleCommand
            {
                ExecuteDelegate = x => Owner.SelectStored()
            };
            cmdSelectInvert = new SimpleCommand
            {
                ExecuteDelegate = x => Owner.SelectInvert()
            };

            cmdSelectAll_M = new SimpleCommand
            {
                ExecuteDelegate = x => Owner.SelectAll_M()
            };
            cmdSelectNone_M = new SimpleCommand
            {
                ExecuteDelegate = x => Owner.SelectNone_M()
            };
            cmdSelectInvert_M = new SimpleCommand
            {
                ExecuteDelegate = x => Owner.SelectInvert_M()
            };
            cmdAtoB = new SimpleCommand
            {
                ExecuteDelegate = x => Owner.CopyAtoB()
            };
            cmdBtoA = new SimpleCommand
            {
                ExecuteDelegate = x => Owner.CopyBtoA()
            };

            cmdCaptureA = new SimpleCommand
            {
                ExecuteDelegate = x => Owner.CaptureA()
            };
            cmdCaptureMissingA = new SimpleCommand
            {
                ExecuteDelegate = x => Owner.CaptureMissingA()
            };
            cmdPurgeA = new SimpleCommand
            {
                ExecuteDelegate = x => Owner.PurgeA()
            };
            cmdClearSelectedA = new SimpleCommand
            {
                ExecuteDelegate = x => Owner.ClearSelectedA()
            };
            cmdClearA = new SimpleCommand
            {
                ExecuteDelegate = x => Owner.ClearA()
            };
            cmdRestoreA = new SimpleCommand
            {
                ExecuteDelegate = x => Owner.RestoreA()
            };

            cmdCaptureB = new SimpleCommand
            {
                ExecuteDelegate = x => Owner.CaptureB()
            };
            cmdCaptureMissingB = new SimpleCommand
            {
                ExecuteDelegate = x => Owner.CaptureMissingB()
            };
            cmdPurgeB = new SimpleCommand
            {
                ExecuteDelegate = x => Owner.PurgeB()
            };
            cmdClearSelectedB = new SimpleCommand
            {
                ExecuteDelegate = x => Owner.ClearSelectedB()
            };
            cmdClearB = new SimpleCommand
            {
                ExecuteDelegate = x => Owner.ClearB()
            };
            cmdRestoreB = new SimpleCommand
            {
                ExecuteDelegate = x => Owner.RestoreB()
            };
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

                case "SlotA":
                    NotifyPropertyChanged("SlotA");
                    foreach (CMachineStateVM s in States)
                    {
                        s.OnPropertyChanged("GotValueA");
                        s.OnPropertyChanged("DisplayValueA");
                    }
                    break;

                case "SlotB":
                    NotifyPropertyChanged("SlotB");
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
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }

        #endregion

        private CMachine Owner { get; set; }

        public string SelectionInfo => Owner.SelectionInfo;

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
        public SimpleCommand cmdCapture { get; private set; }
        public SimpleCommand cmdCaptureMissing { get; private set; }
        public SimpleCommand cmdRestore { get; private set; }
        public SimpleCommand cmdClear { get; private set; }
        public SimpleCommand cmdPurge { get; private set; }
        public SimpleCommand cmdMap { get; private set; }
        public SimpleCommand cmdMapSpecific { get; private set; }
        public SimpleCommand cmdSelectAll { get; private set; }
        public SimpleCommand cmdSelectNone { get; private set; }
        public SimpleCommand cmdSelectStored { get; private set; }
        public SimpleCommand cmdSelectInvert { get; private set; }

        public SimpleCommand cmdSelectAll_M { get; private set; }
        public SimpleCommand cmdSelectNone_M { get; private set; }
        public SimpleCommand cmdSelectInvert_M { get; private set; }
        public SimpleCommand cmdAtoB { get; private set; }
        public SimpleCommand cmdBtoA { get; private set; }

        public SimpleCommand cmdCaptureA { get; private set; }
        public SimpleCommand cmdCaptureMissingA { get; private set; }
        public SimpleCommand cmdPurgeA { get; private set; }
        public SimpleCommand cmdClearSelectedA { get; private set; }
        public SimpleCommand cmdClearA { get; private set; }
        public SimpleCommand cmdRestoreA { get; private set; }

        public SimpleCommand cmdCaptureB { get; private set; }
        public SimpleCommand cmdCaptureMissingB { get; private set; }
        public SimpleCommand cmdPurgeB { get; private set; }
        public SimpleCommand cmdClearSelectedB { get; private set; }
        public SimpleCommand cmdClearB { get; private set; }
        public SimpleCommand cmdRestoreB { get; private set; }
        #endregion Commands

        #region Properties

        public CMachineSnapshot SlotA => Owner.SlotA;

        public CMachineSnapshot SlotB => Owner.SlotB;

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