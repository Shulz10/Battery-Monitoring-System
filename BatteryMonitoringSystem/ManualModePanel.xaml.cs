using System;
using System.Collections.Generic;
using System.Linq;
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
        static ThicknessAnimation animate;
        public static AnimationClock animationClock { get; set; }

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
                animate = new ThicknessAnimation()
                {
                    From = animationClock != null ? (Thickness)animate.GetCurrentValue(animate.From, animate.To, animationClock) : new Thickness(0, 0, 0, 0),
                    To = new Thickness(-350, 0, 0, 0),
                    Duration = TimeSpan.FromSeconds(0.8)
                };
                animationClock = animate.CreateClock();
                animate.Completed += (o, s) => {
                    if (!panel.IsPressed)
                        RemoveUserControl((Panel)LogicalTreeHelper.GetParent(panel), panel);
                };
                panel.BeginAnimation(MarginProperty, animate);
            }
            else if (panel.IsPressed && panel.Parent != null)
            {
                var parent = (Panel)LogicalTreeHelper.GetParent(panel);
                List<UserControl> userControls = parent.Children.OfType<UserControl>().ToList();
                animate = new ThicknessAnimation()
                {
                    From = animationClock != null ? (Thickness)animate.GetCurrentValue(animate.From, animate.To, animationClock) : userControls.Count == 1 ? new Thickness(-350, 0, 0, 0) : new Thickness(-700, 0, 0, 0),
                    To = new Thickness(0, 0, 0, 0),
                    Duration = TimeSpan.FromSeconds(userControls.Count > 1 ? 1 : 0.8)
                };
                animationClock = animate.CreateClock();
                animate.Completed += (o, s) => {
                    if (userControls.Count > 1)
                        RemoveUserControl(parent, userControls[0]);
                };
                panel.BeginAnimation(MarginProperty, animate);
            }
        }

        private static void RemoveUserControl(Panel parent, UserControl panel)
        {
            parent.Children.Remove(panel);
            panel.GetType().GetProperty("animationClock").SetValue(panel, null);
            panel.GetType().GetProperty("IsPressed").SetValue(panel, false);
        }

        private void ManualModeParametersPanel_Loaded(object sender, RoutedEventArgs e) => choosePhoneNumber.SelectionChanged += ChoosePhoneNumber_SelectionChanged;

        public string FormSmsCommand(CommandCode commandCode)
        {
            string command = "";
            int fromN = fromTxt.Text != "" ? Convert.ToInt32(fromTxt.Text) : 0;
            int beforeN = beforeTxt.Text != "" ? Convert.ToInt32(beforeTxt.Text) : 0;
            int messageCount = messageCountTxt.Text != "" ? Convert.ToInt32(messageCountTxt.Text) : 0;
            string phoneNumber =  (choosePhoneNumber.SelectedItem as ComboBoxItem).Content.ToString();

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
                                if (beforeN - fromN + 1 != messageCount)
                                    return "Ошибка! Проверьте правильность введенных данных.";
                            }
                            else
                                return "Ошибка! Проверьте правильность введенных данных.";

                            command = phoneNumber + "2" + ConvertDecimalNumberToHex(fromN, 8) +
                                ConvertDecimalNumberToHex(messageCount, 2) + ConvertDecimalNumberToHex(beforeN, 8);
                        }
                        break;
                    }
                case CommandCode.LastMessage:
                    command = phoneNumber + "0";
                    break;
            }

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
