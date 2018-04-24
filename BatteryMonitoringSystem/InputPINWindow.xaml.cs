using System.Windows;

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
            else MessageBox.Show("Введите PIN!", "Ошибка");
        }
    }
}
