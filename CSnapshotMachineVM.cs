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

            SlotA = Slots[0];
            SlotB = Slots[0];

            States = new ObservableCollection<CMachineStateVM>(
                (from state in Owner.States
                 select new CMachineStateVM(state, CurrentSlot))
                .ToList());

            StatesA = new ObservableCollection<CMachineStateVM>(
                (from state in Owner.States
                 select new CMachineStateVM(state, SlotA))
                .ToList());

            StatesB = new ObservableCollection<CMachineStateVM>(
                (from state in Owner.States
                 select new CMachineStateVM(state, SlotB))
                .ToList());

            Owner.PropertyChanged += OwnerPropertyChanged;

            SelA = 0;
            SelB = 0;

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
        }

        private void OwnerPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "State":
                    NotifyPropertyChanged("CurrentSlot");
                    NotifyPropertyChanged("SlotName");
                    NotifyPropertyChanged("SelectionInfo");
                    foreach (CMachineStateVM s in States)
                    {
                        s.OnPropertyChanged("GotValue");
                    }
                    if(Slot != SelA)
                    {
                        foreach (CMachineStateVM s in StatesA)
                        {
                            s.OnPropertyChanged("GotValue");
                        }
                    }
                    if (Slot != SelB)
                    {
                        foreach (CMachineStateVM s in StatesB)
                        {
                            s.OnPropertyChanged("GotValue");
                        }
                    }
                    break;

                case "Names":
                    foreach (CMachineStateVM s in States)
                    {
                        s.OnPropertyChanged("Name");
                    }
                    if (Slot != SelA)
                    {
                        foreach (CMachineStateVM s in StatesA)
                        {
                            s.OnPropertyChanged("Name");
                        }
                    }
                    if (Slot != SelB)
                    {
                        foreach (CMachineStateVM s in StatesB)
                        {
                            s.OnPropertyChanged("Name");
                        }
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
            States.Add(new CMachineStateVM(state, CurrentSlot));
            StatesA.Add(new CMachineStateVM(state, SlotA));
            StatesB.Add(new CMachineStateVM(state, SlotB));
        }

        public void RemoveState(CMachineState state)
        {
            States.RemoveAt(States.FindIndex(x => x._state == state));
            StatesA.RemoveAt(StatesA.FindIndex(x => x._state == state));
            StatesB.RemoveAt(StatesB.FindIndex(x => x._state == state));
        }

        public ObservableCollection<CMachineStateVM> States { get; }
        public ObservableCollection<CMachineStateVM> StatesA { get; }
        public ObservableCollection<CMachineStateVM> StatesB { get; }

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
        #endregion Commands

        #region Properties
        public CMachineSnapshot SlotA { get; private set; }
        public CMachineSnapshot SlotB { get; private set; }

        private int _selA;
        public int SelA
        {
            get { return _selA; }
            set
            {
                _selA = value;
                SlotA = Owner.Slots[_selA];
                foreach(var s in StatesA)
                {
                    s.Reference = SlotA;
                    s.OnPropertyChanged("GotValue");
                }
                NotifyPropertyChanged("StatesA");
            }
        }

        private int _selB;
        public int SelB
        {
            get { return _selB; }
            set
            {
                _selB = value;
                SlotB = Owner.Slots[_selB];
                foreach (var s in StatesB)
                {
                    s.Reference = SlotB;
                    s.OnPropertyChanged("GotValue");
                }
                NotifyPropertyChanged("StatesB");
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