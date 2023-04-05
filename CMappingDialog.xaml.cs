using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Snapshot
{
    public partial class CMappingDialog : Window, INotifyPropertyChanged
    {
        public CMappingDialog(CMachine owner, CMidiTargetInfo target, CMidiEventSettings settings, bool specific)
        {
            _owner = owner;
            _target = target;

            DataContext = this;

            // These are off for machine level actions
            ShowSelectionCheck = false;
            ShowBoolOption1 = false;

            if (specific)
            {
                switch (target.command)
                {
                    case "Capture":
                        ShowSelectionCheck = true;
                        ShowBoolOption1 = true;
                        BoolOption1Text = "Clear non-selected values";
                        break;

                    case "CaptureMissing":
                        ShowSelectionCheck = true;
                        break;

                    case "Clear":
                        ShowBoolOption1 = true;
                        BoolOption1Text = "Require confirmation";
                        break;

                    case "ClearSelected":
                        ShowBoolOption1 = true;
                        ShowSelectionCheck = true;
                        BoolOption1Text = "Require confirmation";
                        break;

                    case "Purge":
                        ShowBoolOption1 = true;
                        ShowSelectionCheck = true;
                        BoolOption1Text = "Require confirmation";
                        break;

                    case "Restore":
                        break;

                    default:
                        break;
                }
            }

            TypeValues = new List<string>
            {
                "Undefined",
                "Note On",
                "Note Off",
                "Controller"
            };

            ChannelValues = new List<string>();
            for (Byte i = 1; i <= 16; i++)
            {
                ChannelValues.Add(i.ToString());
            }
            ChannelValues.Add("Any");

            PrimaryValues = new List<string>();
            for (Byte i = 1; i <= 128; i++)
            {
                PrimaryValues.Add(i.ToString());
            }
            PrimaryValues.Add("Any");

            SecondaryValues = new List<string>();
            for (Byte i = 1; i <= 128; i++)
            {
                SecondaryValues.Add(i.ToString());
            }
            SecondaryValues.Add("Any");

            Settings = settings;
            _owner.MappingDialogSettings = Settings; // Blocks MIDI events

            InitializeComponent();

            string targetName = target.index < 0 ? "Snaphot" : _owner.Slots[target.index].Name;

            Title = string.Format("{0}->{1}", targetName, target.command);
        }

        private readonly CMidiTargetInfo _target;

        public event PropertyChangedEventHandler PropertyChanged;
        public virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public List<string> TypeValues { get; private set; }
        public List<string> ChannelValues { get; private set; }
        public List<string> PrimaryValues { get; private set; }
        public List<string> SecondaryValues { get; private set; }

        readonly CMachine _owner;

        public CMidiEventSettings Settings { get; set; }

        public bool ShowSelectionCheck { get; set; }

        public bool ShowBoolOption1 { get; set; }
        public string BoolOption1Text { get; set; }

        public bool Learning
        {
            get => _owner.MappingDialogSettings.Learning;
            set
            {
                if(value != _owner.MappingDialogSettings.Learning)
                {
                    _owner.MappingDialogSettings.Learning = value;
                    OnPropertyChanged("Learning");
                    OnPropertyChanged("Settings");
                }
            }
        }

        private void btnOkay_Click(object sender, RoutedEventArgs e)
        {
            // Check for conflicting mappings
            List<CMidiTargetInfo> conflicts = _owner.FindDuplicateMappings(Settings);
            _ = conflicts.Remove(_target);

            if (conflicts.Count > 0)
            {
                string msg = "MIDI event conflicts with mapping for:\n\n";
                foreach(CMidiTargetInfo t in conflicts)
                {
                    string targetName = t.index < 0 ? "Snapshot" : string.Format("Slot {0}", t.index);
                    msg += string.Format("\t{0}->{1}\n", targetName, t.command);
                }
                msg += "\nIs this okay?";
                msg += "\n\n'Yes' to accept conflicts.\n'No' to remove conflicts.\n'Cancel' to edit settings.";
                    
                MessageBoxResult result = MessageBox.Show(msg, "Mapping conflict", MessageBoxButton.YesNoCancel);
                switch(result)
                {
                    case MessageBoxResult.Yes: // Keep conflicts
                        break;

                    case MessageBoxResult.No: // Remove conflicts
                        _owner.RemoveMappings(conflicts);
                        break;

                    case MessageBoxResult.Cancel: // Back to dialog
                        return;

                    default:
                        return; // Shouldn't happen
                }
            }

            DialogResult = true;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void btnLearn_Click(object sender, RoutedEventArgs e)
        {
            Learning = true;
        }
    }
}
