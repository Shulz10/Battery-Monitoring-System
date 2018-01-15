using BatteryMonitoringSystem.Models;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BatteryMonitoringSystem
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Port customComPort;
        private InformationSourcePanel informationSourcePanel;
        private AutoModePanel autoModePanel;
        private ManualModePanel manualModePanel;
        private ComPortSettingsPanel comPortSettingsPanel;
        private List<string> choseInformationSource;
        private List<ShortMessage> unreadShortMessages;
        private string gsmUserPin;

        public MainWindow()
        {
            InitializeComponent();

            informationSourcePanel = new InformationSourcePanel(messagesHistoryDataGrid);
            informationSourcePanel.chooseSourceBtn.Click += (s, e) => { SetInformationSource(); };
            autoModePanel = new AutoModePanel();
            manualModePanel = new ManualModePanel();
            manualModePanel.getRangeMessageBtn.Click += (s, e) => { GetListMessage(manualModePanel.choosePhoneNumber.SelectedItem.ToString(), manualModePanel.FormSmsCommand(CommandCode.RangeMessage)); };
            manualModePanel.getLastMessageBtn.Click += (s, e) => { GetListMessage(manualModePanel.choosePhoneNumber.SelectedItem.ToString(), manualModePanel.FormSmsCommand(CommandCode.LastMessage)); };
            comPortSettingsPanel = new ComPortSettingsPanel();
            comPortSettingsPanel.setComPortSettingsBtn.Click += (s, e) => { AcceptSettings(); };
            gsmUserPin = "";
        }

        private void SetInformationSource()
        {
            choseInformationSource = new List<string>();
            messagesHistoryDataGrid.Items.Clear();
            using (SystemDbContext context = new SystemDbContext(ConfigurationManager.ConnectionStrings["BatteryMonitoringSystemDb"].ConnectionString))
            {
                foreach (var source in informationSourcePanel.sourcePanel.Children.OfType<CheckBox>())
                {
                    if (source.IsChecked ?? false)
                        choseInformationSource.Add((source.Content as TextBlock).Text);
                }
                foreach (var infoSource in choseInformationSource)
                {
                    var query = (from source in context.InformationSources
                                 join info in context.Informations on source.InformationSourceID equals info.InformationSourceID
                                 where source.InternationalCode == infoSource.Substring(0, 6) && source.PhoneNumber == infoSource.Substring(6)
                                 select new
                                 {
                                     MessageNumber = info.MessageNumber,
                                     MessageDateTime = info.MessageDateTime,
                                     Message = info.Message,
                                     PhoneNumber = info.InformationSource.InternationalCode + info.InformationSource.PhoneNumber
                                 }).ToList();

                    if(query.Count > 0)
                        messagesHistoryDataGrid.Items.Add(query);
                }
            }
            MessageBox.Show("Sources of information were successfully selected!", "Congratulations", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void AcceptSettings()
        {
            customComPort = Port.GetComPort();
            if (customComPort.OpenComPort("COM" + comPortSettingsPanel.comPortName.Text, Convert.ToInt32(comPortSettingsPanel.comPortBaudRate.Text)))
                programStatus.Text = "Connected at " + customComPort.CustomSerialPort.PortName;
        }

        private void GetListMessage(string phoneNumber, string command)
        {
            if (customComPort.SendMessage(phoneNumber, ref gsmUserPin, command))
            {
                Thread listeningThread = new Thread(ReceivingResponseToRequest);
                listeningThread.Start();
                programStatus.Text = "Message has sent successfully";
            }
            else
                programStatus.Text = "Failed to send message!";
        }

        private void ReceivingResponseToRequest()
        {
            this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, (ThreadStart)delegate ()
            {
                try
                {
                    if (customComPort.CountSMSMessages(ref gsmUserPin) > 0)
                        unreadShortMessages = customComPort.ReadSMS(ref gsmUserPin);
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            });
        }

        private void OpenListInformationSources(object sender, RoutedEventArgs e)
        {
            if (!informationSourcePanel.IsPressed)
            {
                ChangeButtonBackgroundColor((sender as Button).Name);
                informationSourcePanel.IsPressed = true;
                Grid.SetRow(informationSourcePanel, 1);
                Grid.SetColumn(informationSourcePanel, 1);
                grid.Children.Add(informationSourcePanel);
            }
            else
            {
                (sender as Button).Background = new SolidColorBrush(Color.FromRgb(182, 182, 182));
                informationSourcePanel.IsPressed = false;
            }            
        }

        private void OpenListAutoModeParameters(object sender, RoutedEventArgs e)
        {
            if (!autoModePanel.IsPressed)
            {
                ChangeButtonBackgroundColor((sender as Button).Name);
                autoModePanel.IsPressed = true;
                Grid.SetRow(autoModePanel, 1);
                Grid.SetColumn(autoModePanel, 1);
                grid.Children.Add(autoModePanel);
            }
            else
            {
                (sender as Button).Background = new SolidColorBrush(Color.FromRgb(182, 182, 182));
                autoModePanel.IsPressed = false;
            }
        }

        private void OpenListManualModeParameters(object sender, RoutedEventArgs e)
        {
            if (!manualModePanel.IsPressed)
            {
                ChangeButtonBackgroundColor((sender as Button).Name);
                manualModePanel.IsPressed = true;
                if (choseInformationSource != null)
                {
                    manualModePanel.choosePhoneNumber.Items.Clear();
                    manualModePanel.choosePhoneNumber.Items.Add(new ComboBoxItem() { Visibility = Visibility.Collapsed, Content = "Выберите получателя" });
                    foreach (var source in choseInformationSource)
                        manualModePanel.choosePhoneNumber.Items.Add(source);
                    manualModePanel.choosePhoneNumber.SelectedIndex = 0;
                }
                Grid.SetRow(manualModePanel, 1);
                Grid.SetColumn(manualModePanel, 1);
                grid.Children.Add(manualModePanel);
            }
            else
            {
                (sender as Button).Background = new SolidColorBrush(Color.FromRgb(182, 182, 182));
                manualModePanel.IsPressed = false;
            }
        }

        private void SetComPortSettings(object sender, RoutedEventArgs e)
        {
            if (!comPortSettingsPanel.IsPressed)
            {
                ChangeButtonBackgroundColor((sender as Button).Name);
                comPortSettingsPanel.IsPressed = true;
                Grid.SetRow(comPortSettingsPanel, 1);
                Grid.SetColumn(comPortSettingsPanel, 1);
                grid.Children.Add(comPortSettingsPanel);
            }
            else
            {
                (sender as Button).Background = new SolidColorBrush(Color.FromRgb(182, 182, 182));
                comPortSettingsPanel.IsPressed = false;
            }
        }

        private void OpenFileOfMessages(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog() {
                Filter = "Text files (*.txt)|*.txt",
            };

            if (openFileDialog.ShowDialog() == true)
            {

            }
        }

        private void ChangeButtonBackgroundColor(string btnName)
        {
            foreach (var control in sideMenuPanel.Children)
            {
                if (control is Button)
                {
                    if ((control as Button).Name == btnName)
                        (control as Button).Background = new SolidColorBrush(Color.FromRgb(236, 236, 236));
                    else (control as Button).Background = new SolidColorBrush(Color.FromRgb(182, 182, 182));
                }
            }
        }

        private void SideMenuBtn_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e) => programStatus.Text = (sender as Button).ToolTip.ToString();

        private void SideMenuBtn_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e) => programStatus.Text = "";      
    }
}
