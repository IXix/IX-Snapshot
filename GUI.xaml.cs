using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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

using BuzzGUI.Interfaces;

namespace Snapshot
{
    /// <summary>
    /// Interaction logic for GUI.xaml
    /// </summary>
    public partial class GUI : UserControl, IMachineGUI
    {
        public GUI()
        {
            InitializeComponent();
        }

        public Machine Owner { get; set; }

        IMachine machine;
        public IMachine Machine
        {
            get { return machine; }
            set
            {
                machine = value;

                if (machine != null)
                {
                    Owner = machine.ManagedMachine as Machine;
                    DataContext = Owner.VM;

                    InitControl();
                }
            }
        }

        private void InitControl()
        {
            
        }

        private void OnMachinePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var m = sender as IMachine;

            if (m == machine) // this machine
            {
                switch (e.PropertyName)
                {
                    default:
                        // "Attributes":
                        // "IsActive":
                        // "IsBypassed":
                        // "IsMuted":
                        // "IsSoloed":
                        // "IsWireless":
                        // "LastEngineThread" :
                        // "MIDIInputChannel":
                        // "Name":
                        // "OversampleFactor":
                        // "OverrideLatency":
                        // "Patterns":
                        // "PatternEditorDLL":
                        // "Position":
                        // "TrackCount":
                        break;
                }
            }
            else
            {

                switch (e.PropertyName)
                {
                    case "Name":
                        //UpdateMachineLabels(m);
                        break;

                    default:
                        // "Attributes":
                        // "IsBypassed":
                        // "IsMuted":
                        // "IsSoloed":
                        // "IsActive":
                        // "IsWireless":
                        // "LastEngineThread" :
                        // "MIDIInputChannel":
                        // "OversampleFactor":
                        // "OverrideLatency":
                        // "Patterns":
                        // "PatternEditorDLL":
                        // "Position":
                        // "TrackCount":
                        break;
                }
            }
        }

        private void OnThreeStateClick(object sender, RoutedEventArgs e)
        {
            var chk = sender as CheckBox;
            var VM = chk.DataContext as TreeViewItemViewModel;
            VM.OnClick();
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cb = sender as ComboBox;
            Owner.Slot = cb.SelectedIndex + 1;
        }
    }
}

