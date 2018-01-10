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
using System.Windows.Shapes;

namespace BatteryMonitoringSystem
{
    /// <summary>
    /// Interaction logic for InputPINWindow.xaml
    /// </summary>
    public partial class InputPINWindow : Window
    {
        public InputPINWindow()
        {
            InitializeComponent();
        }

        private void AcceptPIN(object sender, RoutedEventArgs e)
        {
            if (PIN.Text != "" && PIN.Text.Length == 4)
            {
                DialogResult = true;
                Close();
            }
            else MessageBox.Show("Введите PIN!", "Error");
        }
    }
}
