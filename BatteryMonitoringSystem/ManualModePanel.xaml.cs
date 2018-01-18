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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
            this.BeginAnimation(MarginProperty, animate);
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

            switch (commandCode)
            {
                case CommandCode.RangeMessage:
                    {
                        if (fromTxt.Text != "" && beforeTxt.Text == "" && messageCountTxt.Text == "")
                            command = "1" + ConvertDecimalNumberToHex(fromN, 8) + "01" + "00000000";
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

                            command = "2" + ConvertDecimalNumberToHex(fromN, 8) + ConvertDecimalNumberToHex(messageCount, 2) +
                                        ConvertDecimalNumberToHex(beforeN, 8);
                        }
                        break;
                    }
                case CommandCode.LastMessage:
                    command = "0";
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
            if (fromTxt.Text == "" && beforeTxt.Text == "" && messageCountTxt.Text == "")
                getRangeMessageBtn.IsEnabled = false;
            else getRangeMessageBtn.IsEnabled = true;
        }
    }
}
