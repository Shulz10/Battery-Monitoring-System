using BatteryMonitoringSystem.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
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
    /// Interaction logic for NewPhoneWindow.xaml
    /// </summary>
    public partial class NewPhoneWindow : Window
    {
        public NewPhoneWindow()
        {
            InitializeComponent();
        }

        private void AddNewPhone_Click(object sender, RoutedEventArgs e)
        {
            if (phoneNumber.Text != "" && operatorsBox.SelectedIndex > 0)
            {
                using (SystemDbContext context = new SystemDbContext(ConfigurationManager.ConnectionStrings["BatteryMonitoringSystemDb"].ConnectionString))
                {
                    var query = context.InformationSources.Where(p => p.InternationalCode == phoneNumber.Text.Substring(0, 6) &&
                        p.PhoneNumber == phoneNumber.Text.Substring(6)).ToList();
                    if (query.Count == 0)
                    {
                        context.InformationSources.Add(new InformationSource
                        {
                            InternationalCode = phoneNumber.Text.Substring(0, 6),
                            PhoneNumber = phoneNumber.Text.Substring(6),
                            Operator = operatorsBox.Text
                        });
                        context.SaveChanges();
                    }
                }

                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Ошибка ввода данных!");
            }
        }
    }
}
