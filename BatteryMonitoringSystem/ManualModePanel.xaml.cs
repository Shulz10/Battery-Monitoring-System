using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace BatteryMonitoringSystem
{
    /// <summary>
    /// Interaction logic for ManualModePanel.xaml
    /// </summary>
    public partial class ManualModePanel : UserControl
    {
        public ManualModePanel()
        {
            InitializeComponent();            
        }

        public bool IsPressed
        {
            get { return (bool)GetValue(IsPressedProperty); }
            set { SetValue(IsPressedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsPressed.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsPressedProperty =
            DependencyProperty.Register("IsPressed", typeof(bool), typeof(ManualModePanel),
                new PropertyMetadata(false, new PropertyChangedCallback(IsPressedChanged)));

        private static void IsPressedChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs args)
        {
            ManualModePanel panel = (ManualModePanel)depObj;
            if (!panel.IsPressed && panel.Parent != null)
            {
                ThicknessAnimation animate = new ThicknessAnimation()
                {
                    To = new Thickness(-350, 0, 0, 0),
                    Duration = TimeSpan.FromSeconds(0.75)
                };
                animate.Completed += (o, s) => { RemoveUserControl(panel); };
                panel.BeginAnimation(MarginProperty, animate);
            }
        }

        private static void RemoveUserControl(ManualModePanel panel)
        {
            var parent = (Panel)LogicalTreeHelper.GetParent(panel);
            parent.Children.Remove(panel);
        }

        private void ManualModeParametersPanel_Loaded(object sender, RoutedEventArgs e)
        {
            ThicknessAnimation animate = new ThicknessAnimation()
            {
                From = new Thickness(-700, 0, 0, 0),
                To = new Thickness(0, 0, 0, 0),
                Duration = TimeSpan.FromSeconds(1)
            };
            animate.Completed += (o, s) => { RemoveOldPanel(); };
            BeginAnimation(MarginProperty, animate);
            choosePhoneNumber.SelectionChanged += ChoosePhoneNumber_SelectionChanged;
        }

        private void RemoveOldPanel()
        {
            var parent = (Panel)LogicalTreeHelper.GetParent(this);
            foreach (var element in parent.Children)
            {
                if (element is UserControl)
                {
                    if (Grid.GetRow((UserControl)element) == 1 && Grid.GetColumn((UserControl)element) == 1 && ((UserControl)element).Name != this.Name)
                    {
                        parent.Children.Remove((UserControl)element);
                        element.GetType().GetProperty("IsPressed").SetValue(element, false);
                        break;
                    }
                }
            }
        }

        public string FormSmsCommand(CommandCode commandCode)
        {
            string command = "";
            int fromN = fromTxt.Text != "" ? Convert.ToInt32(fromTxt.Text) : 0;
            int beforeN = beforeTxt.Text != "" ? Convert.ToInt32(beforeTxt.Text) : 0;
            int messageCount = messageCountTxt.Text != "" ? Convert.ToInt32(messageCountTxt.Text) : 0;
            string phoneNumber =  (choosePhoneNumber.SelectedItem as ComboBoxItem).Content.ToString();

            if (phoneNumber.Length == 13 && phoneNumber.StartsWith("+"))
            {
                switch (commandCode)
                {
                    case CommandCode.RangeMessage:
                        {
                            if (fromTxt.Text != "" && beforeTxt.Text == "" && messageCountTxt.Text == "")
                            {
                                command = phoneNumber + "1" + ConvertDecimalNumberToHex(fromN, 8) + "01" + "00000000";
                                messageCountTxt.Text = "1";
                                beforeTxt.Text = fromTxt.Text;
                            }
                            else
                            {
                                if (fromTxt.Text != "" && beforeTxt.Text != "" && messageCountTxt.Text == "")
                                {
                                    messageCount = beforeN - fromN + 1;
                                    messageCountTxt.Text = messageCount.ToString();
                                }
                                else if (fromTxt.Text != "" && beforeTxt.Text == "" && messageCountTxt.Text != "")
                                {
                                    beforeN = fromN + messageCount;
                                    beforeTxt.Text = beforeN.ToString();
                                }
                                else if (fromTxt.Text != "" && beforeTxt.Text != "" && messageCountTxt.Text != "")
                                {
                                    if (beforeN - fromN + 1 != messageCount && messageCount != 1)
                                        return "Ошибка! Проверьте правильность введенных данных.";
                                }

                                command = phoneNumber + "2" + ConvertDecimalNumberToHex(fromN, 8) +
                                    ConvertDecimalNumberToHex(messageCount, 2) + ConvertDecimalNumberToHex(beforeN, 8);
                            }
                            break;
                        }
                    case CommandCode.LastMessage:
                        command = phoneNumber + "0";
                        break;
                }
            }
            else return "Ошибка! Проверьте правильность введенных данных.";

            return command;
        }

        private string ConvertDecimalNumberToHex(int number, int resultSymbolCount)
        {
            string resultNumber = number.ToString("X");
            if(resultNumber.Length < resultSymbolCount)
            {
                while (resultNumber.Length < resultSymbolCount)
                    resultNumber = "0" + resultNumber;
            }

            return resultNumber;
        }

        private void CommandParameterChanged(object sender, TextChangedEventArgs e)
        {
            if ((choosePhoneNumber.SelectedItem as ComboBoxItem).IsEnabled && (choosePhoneNumber.SelectedItem as ComboBoxItem).Content.ToString() != "Выберите получателя" &&
                (fromTxt.Text != "" || beforeTxt.Text != "" || messageCountTxt.Text != ""))
                getRangeMessageBtn.IsEnabled = true;
            else
                getRangeMessageBtn.IsEnabled = false;
        }

        private void CheckEnteredCharacter(object sender, TextCompositionEventArgs e)
        {
            if (!Char.IsDigit(e.Text, 0))
                e.Handled = true;
        }

        public void ChangeButtonsAvailability()
        {
            getLastMessageBtn.IsEnabled ^= true;
            if (fromTxt.Text != "" ^ beforeTxt.Text != "" ^ messageCountTxt.Text != "")
                getRangeMessageBtn.IsEnabled ^= true;
        }

        private void ChoosePhoneNumber_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBoxItem comboBoxItem = choosePhoneNumber.SelectedItem as ComboBoxItem;
            if (comboBoxItem != null)
            {
                if (comboBoxItem.IsEnabled && comboBoxItem.Content.ToString() != "Выберите получателя" && (fromTxt.Text != "" || beforeTxt.Text != "" || messageCountTxt.Text != ""))
                {
                    getLastMessageBtn.IsEnabled = true;
                    getRangeMessageBtn.IsEnabled = true;
                }
                else if ((comboBoxItem.IsEnabled && comboBoxItem.Content.ToString() != "Выберите получателя") && (fromTxt.Text == "" || beforeTxt.Text == "" || messageCountTxt.Text == ""))
                {
                    getLastMessageBtn.IsEnabled = true;
                    getRangeMessageBtn.IsEnabled = false;
                }
                else
                {
                    getLastMessageBtn.IsEnabled = false;
                    getRangeMessageBtn.IsEnabled = false;
                }
            }
        }
    }
}
