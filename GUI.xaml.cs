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
            
            // Cheap and nasty way to keep the machine data info up to date.
            // Might have to come up with something better at some point.
            IsKeyboardFocusWithinChanged += (sender, e) =>
            {
                if (Owner != null)
                {
                    Owner.UpdateSizeInfo();
                }
            };
        }

        public CMachine Owner { get; set; }

        IMachine machine;
        public IMachine Machine
        {
            get { return machine; }
            set
            {
                machine = value;

                if (machine != null)
                {
                    Owner = machine.ManagedMachine as CMachine;
                    DataContext = Owner.VM;

                    InitControl();
                }
            }
        }

        private void InitControl()
        {
            
        }

        private void OnThreeStateClick(object sender, RoutedEventArgs e)
        {
            var chk = sender as CheckBox;
            var VM = chk.DataContext as CTreeViewItemVM;
            VM.OnClick();
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cb = sender as ComboBox;
            Owner.Slot = cb.SelectedIndex + 1;
        }
    }
}

