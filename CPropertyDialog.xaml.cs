using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    /// <summary>
    /// Interaction logic for CPropertyDialog.xaml
    /// </summary>
    public partial class CPropertyDialog : Window
    {
        public CPropertyDialog(CMachinePropertyItemVM vm)
        {
            DataContext = vm;

            InitializeComponent();

            Owner = vm._ownerVM.Window;

            // Position of the mouse relative to the window
            this.Loaded += (s, e) =>
            {
                Point p = Mouse.GetPosition(this);
                Title = "Properties: " + vm.Name;
                Top += p.Y - Height / 2;
                Left += p.X - Width / 2;
            };
        }

        private void PropertyDialog1_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Crappy faux-modal behaviour
            Owner.IsEnabled = true;
        }

        private void PropertyDialog1_Loaded(object sender, RoutedEventArgs e)
        {
            // Crappy faux-modal behaviour
            Owner.IsEnabled = false;
        }

        private void btnOkay_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
