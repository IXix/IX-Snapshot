using System;
using System.Collections.Generic;
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
    public partial class CMappingDialog : Window
    {
        public CMappingDialog(string name, CMidiEvent settings)
        {
            DataContext = this;

            TypeValues = new List<string>();
            TypeValues.Add("Undefined");
            TypeValues.Add("Note On");
            TypeValues.Add("Note Off");
            TypeValues.Add("Controller");

            ChannelValues = new List<string>();
            for (Byte i = 0; i < 16; i++)
            {
                ChannelValues.Add(i.ToString());
            }
            ChannelValues.Add("Any");

            PrimaryValues = new List<string>();
            for (Byte i = 0; i < 128; i++)
            {
                PrimaryValues.Add(i.ToString());
            }
            PrimaryValues.Add("Undefined");

            SecondaryValues = new List<string>();
            for (Byte i = 0; i < 128; i++)
            {
                SecondaryValues.Add(i.ToString());
            }
            SecondaryValues.Add("Undefined");

            Command = name;
            EventType = (Byte) settings.Message;
            Channel = settings.Channel;
            Primary = settings.Primary;
            Secondary = settings.Secondary;

            InitializeComponent();
        }

        public List<string> TypeValues { get; private set; }
        public List<string> ChannelValues { get; private set; }
        public List<string> PrimaryValues { get; private set; }
        public List<string> SecondaryValues { get; private set; }

        public string Command { get; set; }
        public Byte EventType { get; set; }
        public Byte Channel { get; set; }
        public Byte Primary { get; set; }
        public Byte Secondary { get; set; }

        private void btnOkay_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
