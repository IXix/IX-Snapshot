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
        public CMappingDialog(CMachine owner, string command, string target, CMidiEvent settings)
        {
            _owner = owner;

            DataContext = this;

            TypeValues = new List<string>();
            TypeValues.Add("Undefined");
            TypeValues.Add("Note On");
            TypeValues.Add("Note Off");
            TypeValues.Add("Controller");

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
            PrimaryValues.Add("Undefined");

            SecondaryValues = new List<string>();
            for (Byte i = 1; i <= 128; i++)
            {
                SecondaryValues.Add(i.ToString());
            }
            SecondaryValues.Add("Any");

            Command = command;
            Settings = settings;
            _owner.MappingDialogSettings = Settings; // Blocks MIDI events

            InitializeComponent();

            Title = string.Format("{0}->{1}", target, command);
        }

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

        public string Command { get; set; }
        public CMidiEvent Settings { get; set; }

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
