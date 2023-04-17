using BuzzGUI.Common;
using System.ComponentModel;

namespace Snapshot
{
    public class CMidiTargetInfo : INotifyPropertyChanged
    {
        public readonly int index; // slot index or -1 for machine
        public readonly string command; // "Capture" etc.
        public readonly object target;

        public CMidiAction action;
        public CMidiEventSettings settings;

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public CMidiTargetInfo(int idx, string cmd, CMachine owner)
        {
            index = idx;
            command = cmd;
            action = null;
            settings = new CMidiEventSettings(owner);

            if (index < 0)
            {
                target = owner;
            }
            else
            {
                target = owner.Slots[idx];
            }


            CmdEdit = new SimpleCommand
            {
                CanExecuteDelegate = x => true,
                ExecuteDelegate = x => { owner.MapCommand(this); }
            };
            CmdRemove = new SimpleCommand
            {
                CanExecuteDelegate = x => true,
                ExecuteDelegate = x => {
                    owner.RemoveMapping(this);
                    owner.OnPropertyChanged("MachineMidi");
                    owner.OnPropertyChanged("SlotMidi");
                }
            };
        }

        internal void SetAction()
        {
            if (index < 0)
            {
                action = new CMidiAction(target, command, settings);
            }
            else
            {
                switch (command)
                {
                    case "Capture":
                        action = new CMidiActionSelectionBool(target, command, settings);
                        break;

                    case "CaptureMissing":
                        action = new CMidiActionSelectionBool(target, command, settings);
                        break;

                    case "ClearSelected":
                        action = new CMidiActionSelectionBool(target, command, settings);
                        break;

                    case "Clear":
                        action = new CMidiActionBool(target, command, settings);
                        break;

                    case "Purge":
                        action = new CMidiActionSelectionBool(target, command, settings);
                        break;

                    default:
                        action = new CMidiAction(target, command, settings);
                        break;
                }
            }

            NotifyPropertyChanged("EventDetails");
        }

        public override int GetHashCode()
        {
            return string.Format("{0}{1}", index, command).GetHashCode();
        }

        public override bool Equals(object obj)
        {
            CMidiTargetInfo that = obj as CMidiTargetInfo;
            if (that != null)
            {
                return index == that.index && command == that.command;
            }
            return false;
        }

        public string Description
        {
            get
            {
                string targetName = index < 0 ? "" : string.Format("Slot {0} -> ", index);
                return targetName + command;
            }
        }

        public string EventDetails => settings.Description;

        public SimpleCommand CmdEdit { get; private set; }
        public SimpleCommand CmdRemove { get; private set; }
    }
}