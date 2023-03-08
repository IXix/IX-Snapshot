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
        public CPropertyDialog(CMachinePropertyItemVM itemVM)
        {
            DataContext = itemVM;

            InitializeComponent();

            // Position of the mouse relative to the window
            this.Loaded += (s, e) =>
            {
                Point p = Mouse.GetPosition(this);
                Title = "Properties: " + itemVM.Name;
                Top += p.Y - Height / 2;
                Left += p.X - Width / 2;
            };
        }

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
