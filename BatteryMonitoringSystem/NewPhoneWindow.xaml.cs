using BatteryMonitoringSystem.Models;
using System.Configuration;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;

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
                Regex phoneNumberRegex = new Regex(@"^(\+375)(29|25|33|44)(\d{7})$");
                if (phoneNumberRegex.IsMatch(phoneNumber.Text))
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

                            DialogResult = true;
                            Close();
                        }
                        else
                            MessageBox.Show("Введеный номер уже имеется в списке доступных контактов.","Внимание");
                    }                    
                }
                else
                    MessageBox.Show("Неправильно набран номер. Введите номер используя следующую форму записи - +375291234567.", "Ошибка");
            }
            else if (phoneNumber.Text != "" && operatorsBox.SelectedIndex == 0)
                MessageBox.Show("Выберите мобильного оператора!", "Ошибка");
            else if (phoneNumber.Text == "" && operatorsBox.SelectedIndex > 0)
                MessageBox.Show("Введите номер телефона!", "Ошибка");
            else
                MessageBox.Show("Данные не введены.","Ошибка");
        }
    }
}
