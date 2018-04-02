using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using BatteryMonitoringSystem.Models;
using Excel = Microsoft.Office.Interop.Excel;

namespace BatteryMonitoringSystem
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Port customComPort;
        private InformationSourcePanel informationSourcePanel;
        private ManualModePanel manualModePanel;
        private List<string> choseInformationSource;
        //private Queue<string> sourceRequestMessages;
        private List<ShortMessage> unreadShortMessages;
        private int currentMessageCount;
        private int maxMessageCountInStorage;
        private string gsmUserPin;
        private double messagesHistoryListViewActualWidth;
        private Excel.Application excelApp;
        static Barrier barrier = new Barrier(3);
        private Timer[] timers = new Timer[10];

        private Dictionary<string, Tuple<SmsRequest, Timer>> requests;

        public MainWindow()
        {
            InitializeComponent();       
            
            informationSourcePanel = new InformationSourcePanel(messagesHistoryView);
            informationSourcePanel.chooseSourceBtn.Click += (s, e) => { SetInformationSource(); };
            manualModePanel = new ManualModePanel();
            manualModePanel.getRangeMessageBtn.Click += (s, e) => {
                GetListMessage((manualModePanel.choosePhoneNumber.SelectedItem as ComboBoxItem).Content.ToString(),
                    manualModePanel.FormSmsCommand(CommandCode.RangeMessage));
            };
            manualModePanel.getLastMessageBtn.Click += (s, e) => {
                GetListMessage((manualModePanel.choosePhoneNumber.SelectedItem as ComboBoxItem).Content.ToString(),
                    manualModePanel.FormSmsCommand(CommandCode.LastMessage));
            };

            gsmUserPin = "";
            requests = new Dictionary<string, Tuple<SmsRequest, Timer>>();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                customComPort = Port.GetComPort();
                customComPort.OpenComPort();            
                programStatus.Text = $"Подключение установлено по порту {customComPort.CustomSerialPort.PortName}";
                customComPort.GetCountSMSMessagesInStorage(out currentMessageCount, out maxMessageCountInStorage);
            }
            catch(Exception ex)
            {
                if (ex is UnauthorizedAccessException)
                    MessageBox.Show($"Доступ к порту {customComPort.CustomSerialPort.PortName} запрещен", "Ошибка"); //Access to the port COM4 is denied
                else MessageBox.Show(ex.Message, "Ошибка");
                programStatus.Text = "Подключение не установлено. Проверьте соединение с GSM модемом.";
            }
        }

        private void SetInformationSource()
        {
            choseInformationSource = new List<string>();
            messagesHistoryView.Items.Clear();
            using (SystemDbContext context = new SystemDbContext(ConfigurationManager.ConnectionStrings["BatteryMonitoringSystemDb"].ConnectionString))
            {
                foreach (var source in informationSourcePanel.sourcePanel.Children.OfType<CheckBox>())
                {
                    if (source.IsChecked ?? false)
                        choseInformationSource.Add((source.Content as TextBlock).Text);
                }
                if (choseInformationSource.Count > 0)
                {
                    foreach (var infoSource in choseInformationSource)
                    {
                        var query = (from source in context.InformationSources
                                     join info in context.Informations on source.InformationSourceID equals info.InformationSourceID
                                     where source.InternationalCode == infoSource.Substring(0, 6) && source.PhoneNumber == infoSource.Substring(6)
                                     select new ShortMessage
                                     {
                                         MessageNumber = info.MessageNumber,
                                         ReceivedDateTime = info.MessageDateTime,
                                         Message = info.Message,
                                         Sender = info.InformationSource.InternationalCode + info.InformationSource.PhoneNumber
                                     }).ToList();

                        if (query.Count > 0)
                            foreach(ShortMessage msg in query)
                                messagesHistoryView.Items.Add(msg);
                    }
                    programStatus.Text = "Источники информации успешно выбраны!";
                }
                else
                    MessageBox.Show("None of the information sources is selected!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void GetListMessage(string phoneNumber, string command)
        {
            try
            {
                if (command.StartsWith("Ошибка"))
                    programStatus.Text = command;
                else
                {
                    int countExpectedMessages;
                    if (command == phoneNumber + "0")
                        countExpectedMessages = 1;
                    else countExpectedMessages = Convert.ToInt32(manualModePanel.messageCountTxt.Text);

                    if (currentMessageCount + countExpectedMessages * 2 > maxMessageCountInStorage)
                        customComPort.ClearMessageStorage();

                    requests.Add(phoneNumber, Tuple.Create(new SmsRequest(manualModePanel.fromTxt.Text, manualModePanel.beforeTxt.Text, countExpectedMessages > 1 ? CommandCode.RangeMessage : CommandCode.LastMessage),
                        new Timer(ReceivingResponseToRequest, phoneNumber, 0, 60000)));

                    customComPort.SendMessage(phoneNumber, ref gsmUserPin, command);
                    //sourceRequestMessages = new Queue<string>();
                    //sourceRequestMessages.Enqueue(phoneNumber);

                    /*int index = Array.IndexOf(timers, null);
                    if (index != -1)
                    {
                        timers[index] = new Timer(ReceivingResponseToRequest, phoneNumber, 0, 60000);
                    }*/

                    programStatus.Text = "Сообщение отправлено успешно";//Message has sent successfully
                }
            }
            catch(Exception ex)
            {
                programStatus.Text = ex.Message;
            }
        }

        private void ReceivingResponseToRequest(object phoneNumber)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate ()
            {
                try
                {
                    customComPort.GetCountSMSMessagesInStorage(out int messageCount);
                    if (messageCount - currentMessageCount > 0)
                    {
                        currentMessageCount = messageCount;
                        unreadShortMessages = customComPort.ReadSMS(ref gsmUserPin);
                        unreadShortMessages = unreadShortMessages.FindAll(msg => msg.Sender == phoneNumber as string);
                        if (unreadShortMessages != null)
                        {
                            foreach (var msg in unreadShortMessages)
                                messagesHistoryView.Items.Add(new
                                {
                                    MessageNumber = msg.MessageNumber,
                                    MessageDateTime = msg.ReceivedDateTime,
                                    Message = msg.Message,
                                    PhoneNumber = msg.Sender
                                });
                        }
                    }
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
                        manualModePanel.choosePhoneNumber.Items.Add(new ComboBoxItem() { Content = source });
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

        private async void OpenFileOfMessages(object sender, RoutedEventArgs e)
        {
            operationProgress.Visibility = Visibility.Visible;
            dataLoading.Value = 0;
            
            await Task.Run(async () =>
            {
                IEnumerable<DriveInfo> drivesInfo = DriveInfo.GetDrives().Where(drive => drive.DriveType == DriveType.Removable);
                foreach (var drive in drivesInfo)
                {
                    if (drive.IsReady)
                    {
                        string filePath = Directory.GetFiles(string.Format(@"{0}", drive.Name), "+375*").FirstOrDefault();
                        if (filePath != null)
                        {
                            FileInfo smsHistoryFile = new FileInfo(filePath);
                            double stepValue = await GetStepValueForProgressOperation(filePath);
                            using (StreamReader streamReader = smsHistoryFile.OpenText())
                            {
                                string sms;
                                while((sms = streamReader.ReadLine()) != null)
                                {
                                    messagesHistoryView.Items.Add(new ShortMessage(smsHistoryFile.Name.Replace(".txt", ""), sms));
                                    await dataLoading.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                                        new DispatcherOperationCallback(delegate(object value)
                                        {
                                            dataLoading.Value += Convert.ToDouble(value);
                                            return null;
                                        }), stepValue);
                                }
                            }
                        }
                    }
                }
            });
            operationProgress.Visibility = Visibility.Hidden;
            programStatus.Text = "Файл успешно выгружен в таблицу.";
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

        private void MessagesHistoryView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.WidthChanged)
            {
                GridView view = this.messagesHistoryView.View as GridView;
                Decorator border = VisualTreeHelper.GetChild(this.messagesHistoryView, 0) as Decorator;
                if(border != null)
                {
                    ScrollViewer scroller = border.Child as ScrollViewer;
                    if(scroller != null)
                    {
                        ItemsPresenter presenter = scroller.Content as ItemsPresenter;
                        if(presenter != null)
                        {
                            for(int i = 0; i < view.Columns.Count; i++)
                            {
                                double percentWidth = view.Columns[i].Width * 100 / messagesHistoryListViewActualWidth;
                                view.Columns[i].Width = percentWidth * presenter.ActualWidth / 100;
                            }
                            messagesHistoryListViewActualWidth = presenter.ActualWidth;
                        }
                    }
                }                  
            }
        }

        private void MessagesHistoryView_Loaded(object sender, RoutedEventArgs e)
        {
            GridView view = this.messagesHistoryView.View as GridView;
            Decorator border = VisualTreeHelper.GetChild(this.messagesHistoryView, 0) as Decorator;
            if (border != null)
            {
                ScrollViewer scroller = border.Child as ScrollViewer;
                if (scroller != null)
                {
                    ItemsPresenter presenter = scroller.Content as ItemsPresenter;
                    if (presenter != null)
                    {
                        view.Columns[4].Width = presenter.ActualWidth;
                        for (int i = 0; i < view.Columns.Count; i++)
                            if (i != 4) view.Columns[4].Width -= view.Columns[i].ActualWidth;
                    }
                    messagesHistoryListViewActualWidth = presenter.ActualWidth;
                }
            }
            this.messagesHistoryView.SizeChanged += MessagesHistoryView_SizeChanged;
        }

        private Task<double> GetStepValueForProgressOperation(string filePath)
        {
            return Task.Run(() =>
            {
                double stepsCount = File.ReadAllLines(filePath).Count();
                return 100 / stepsCount;
            });
        }

        private void SaveDataToExcelFile(object sender, RoutedEventArgs e)
        {
            var taskFactory = new TaskFactory();
            Task task1 = taskFactory.StartNew(CreateExcelFile);
            //for(int i = 0; i < choseInformationSource.Count; i++)
                Task task2 = taskFactory.StartNew<List<ShortMessage>>(GetListMessageForPhoneNumber, choseInformationSource[0]);
            barrier.SignalAndWait();

            if (choseInformationSource.Count > 0)
            {
                excelApp = new Excel.Application();
                excelApp.SheetsInNewWorkbook = choseInformationSource.Count;
                var excelWorkbook = excelApp.Workbooks.Add();
                int index = 0;
                foreach (Excel.Worksheet sheet in excelApp.Worksheets)
                {
                    sheet.Name = choseInformationSource[index];
                    sheet.Cells[1, "A"] = "№";
                    sheet.Columns["A"].ColumnWidth = 5; 
                    sheet.Cells[1, "B"] = "Дата";
                    sheet.Columns["B"].ColumnWidth = 30;
                    sheet.Cells[1, "C"] = "Время";
                    sheet.Columns["C"].ColumnWidth = 30;
                    sheet.Cells[1, "D"] = "Сообщение";
                    sheet.Columns["D"].ColumnWidth = 100;
                    index++;
                }

                for (int k = 0; k < choseInformationSource.Count; k++)
                {
                    List<ShortMessage> listShortMessage = new List<ShortMessage>();
                    foreach (ShortMessage shortMessage in messagesHistoryView.Items)
                    {
                        if(shortMessage.Sender == choseInformationSource[k])
                            listShortMessage.Add(shortMessage);
                    }

                    var rowIndex = 1;
                    foreach (var row in listShortMessage)
                    {
                        rowIndex++;
                        excelApp.Worksheets[choseInformationSource[k]].Cells[rowIndex, "A"] = row.MessageNumber;
                        excelApp.Worksheets[choseInformationSource[k]].Cells[rowIndex, "B"] = row.ReceivedDateTime.Date;
                        excelApp.Worksheets[choseInformationSource[k]].Cells[rowIndex, "C"] = row.ReceivedDateTime.TimeOfDay.ToString();
                        excelApp.Worksheets[choseInformationSource[k]].Cells[rowIndex, "D"] = row.Message;
                    }

                }

                excelWorkbook.SaveAs(Environment.CurrentDirectory + "\\" + DateTime.Now.ToString("dd-MM-yyyy H-mm-ss") + ".xlsx", Type.Missing, Type.Missing, Type.Missing, Type.Missing,
                    Type.Missing, Excel.XlSaveAsAccessMode.xlNoChange, Type.Missing, Type.Missing, Type.Missing, Type.Missing);

                programStatus.Text = "Excel файл успешно создан!";
                excelApp.Quit();
            }
        }

        private void CreateExcelFile()
        {
            excelApp = new Excel.Application();
            excelApp.SheetsInNewWorkbook = choseInformationSource.Count;
            var excelWorkbook = excelApp.Workbooks.Add();
            int index = 0;
            foreach (Excel.Worksheet sheet in excelApp.Worksheets)
            {
                sheet.Name = choseInformationSource[index];
                sheet.Cells[1, "A"] = "№";
                sheet.Columns["A"].ColumnWidth = 5;
                sheet.Cells[1, "B"] = "Дата";
                sheet.Columns["B"].ColumnWidth = 30;
                sheet.Cells[1, "C"] = "Время";
                sheet.Columns["C"].ColumnWidth = 30;
                sheet.Cells[1, "D"] = "Сообщение";
                sheet.Columns["D"].ColumnWidth = 100;
                index++;
            }
            barrier.SignalAndWait();
        }

        private List<ShortMessage> GetListMessageForPhoneNumber(object phoneNumber)
        {
            List<ShortMessage> listShortMessage = new List<ShortMessage>();
            foreach (ShortMessage shortMessage in messagesHistoryView.Items)
            {
                if (shortMessage.Sender == phoneNumber.ToString())
                    listShortMessage.Add(shortMessage);
            }
            return listShortMessage;
        }
    }
}
