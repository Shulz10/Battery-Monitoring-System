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
        private CurrentRequestsPanel currentRequestsPanel;
        private List<string> choseInformationSource;
        private List<ShortMessage> unreadShortMessages;
        private int maxMessageCountInStorage;
        private string gsmUserPin;
        private double messagesHistoryListViewActualWidth;
        private Excel.Application excelApp;
        private DispatcherTimer updateDiffRequestTime;
        private DispatcherTimer updateStatisticsByReceivedMessage;

        private Dictionary<string, Tuple<SmsRequest, Timer>> requests;

        public MainWindow()
        {
            InitializeComponent();       
            
            informationSourcePanel = new InformationSourcePanel(messagesHistoryView);
            informationSourcePanel.chooseSourceBtn.Click += (s, e) => { SetInformationSource(); };
            manualModePanel = new ManualModePanel();
            manualModePanel.getRangeMessageBtn.Click += SendRequest;
            manualModePanel.getLastMessageBtn.Click += SendRequest;
            currentRequestsPanel = new CurrentRequestsPanel();

            gsmUserPin = "";
            requests = new Dictionary<string, Tuple<SmsRequest, Timer>>();
        }       

        private void ComPortInitialization()
        {
            try
            {
                customComPort = Port.GetComPort();
                if (!customComPort.CustomSerialPort.IsOpen)
                {
                    customComPort.OpenComPort();
                    programStatus.Text = $"Подключение установлено по порту {customComPort.CustomSerialPort.PortName}";
                }
              
                customComPort.EnterSimCardPin(ref gsmUserPin);
                programStatus.Text = $"PIN введён верно";
                Task.Delay(3000).Wait();
                
                customComPort.GetCountSMSMessagesInStorage(out int currentMessageCount, out maxMessageCountInStorage);
                if (currentMessageCount > 0)
                    customComPort.ClearMessageStorage();
                programStatus.Text = $"Инициализация хранилища сообщений выполнена";
            }
            catch (Exception ex)
            {
                throw ex;
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
                            AddMessagesFromDb(query);

                        programStatus.Text = "Источники информации успешно выбраны!";
                    }
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

                    if (requests.Values.Sum(n => n.Item1.MessagesNumber - n.Item1.ReceivedMessagesNumber) + countExpectedMessages * 2 <= maxMessageCountInStorage && requests.Count <= 5)
                    {
                        customComPort.SendMessage(phoneNumber, command);

                        requests.Add(phoneNumber, Tuple.Create(new SmsRequest(manualModePanel.fromTxt.Text, manualModePanel.beforeTxt.Text, DateTime.Now, countExpectedMessages > 1 ? CommandCode.RangeMessage : CommandCode.LastMessage),
                            new Timer(ReceivingResponseToRequest, phoneNumber, 0, 60000)));

                        (manualModePanel.choosePhoneNumber.SelectedItem as ComboBoxItem).IsEnabled = false;

                        programStatus.Text = "Сообщение отправлено успешно";//Message has sent successfully

                        manualModePanel.ChangeButtonsAvailability();
                    }
                    else programStatus.Text = "Отправка запроса невозможна. Отмените не нужный запрос или дождитесь окончания.";
                }
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }

        private async void ReceivingResponseToRequest(object fromObject)
        {
            await Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)async delegate ()
            {
                try
                {
                    string phoneNumber = fromObject as string;
                    customComPort.GetCountSMSMessagesInStorage(out int messageCount);
                    if (messageCount > 0)
                    {
                        unreadShortMessages = customComPort.ReadSMS();
                        unreadShortMessages = unreadShortMessages.FindAll(msg => msg.Sender == phoneNumber);

                        if (unreadShortMessages != null)
                        {
                            requests[phoneNumber].Item1.ReceivedMessagesNumber += unreadShortMessages.Count;
                            operationProgress.Visibility = Visibility.Visible;
                            dataLoading.Value = 0;
                            await Task.Run(() => WriteSmsInDb(unreadShortMessages));

                            dataLoading.Value = 0;
                            AddNewMessageToDataTable(unreadShortMessages);
                            customComPort.RemoveMessagesByNumber(unreadShortMessages);

                            await Task.Delay(1500);
                            operationProgress.Visibility = Visibility.Hidden;
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
            if (!informationSourcePanel.IsPressed && !grid.Children.Contains(informationSourcePanel))
            {            
                Grid.SetRow(informationSourcePanel, 1);
                Grid.SetColumn(informationSourcePanel, 1);
                grid.Children.Add(informationSourcePanel);
                ChangeButtonBackgroundColor((sender as Button).Name);
                informationSourcePanel.IsPressed = true;
            }
            else if(informationSourcePanel.IsPressed)
            {
                (sender as Button).Background = new SolidColorBrush(Color.FromRgb(182, 182, 182));
                informationSourcePanel.IsPressed = false;
            }            
        }

        private void OpenListManualModeParameters(object sender, RoutedEventArgs e)
        {
            if (!manualModePanel.IsPressed && !grid.Children.Contains(manualModePanel))
            {                
                if (choseInformationSource != null)
                {
                    manualModePanel.choosePhoneNumber.Items.Clear();
                    manualModePanel.choosePhoneNumber.Items.Add(new ComboBoxItem() { Visibility = Visibility.Collapsed, Content = "Выберите получателя" });
                    foreach (var source in choseInformationSource)
                        manualModePanel.choosePhoneNumber.Items.Add(new ComboBoxItem() { Content = source, IsEnabled = requests.ContainsKey(source) ? false : true });
                    manualModePanel.choosePhoneNumber.SelectedIndex = 0;
                }
                Grid.SetRow(manualModePanel, 1);
                Grid.SetColumn(manualModePanel, 1);
                grid.Children.Add(manualModePanel);
                ChangeButtonBackgroundColor((sender as Button).Name);
                manualModePanel.IsPressed = true;
            }
            else if(manualModePanel.IsPressed)
            {
                (sender as Button).Background = new SolidColorBrush(Color.FromRgb(182, 182, 182));
                manualModePanel.IsPressed = false;
            }
        }

        private void SendRequest(object sender, RoutedEventArgs e)
        {
            try
            {
                if(customComPort == null)
                    ComPortInitialization();
                GetListMessage((manualModePanel.choosePhoneNumber.SelectedItem as ComboBoxItem).Content.ToString(),
                    manualModePanel.FormSmsCommand((sender as Button).Name == "getLastMessageBtn" ? CommandCode.LastMessage : CommandCode.RangeMessage));
            }
            catch(Exception ex)
            {
                if (ex is UnauthorizedAccessException)
                {
                    MessageBox.Show($"Доступ к порту {customComPort.CustomSerialPort.PortName} запрещен", "Ошибка"); //Access to the port COM4 is denied
                    programStatus.Text = "Подключение не установлено. Проверьте соединение с GSM модемом.";
                }
                else programStatus.Text = ex.Message;
            }
        }

        private void OpenListCurrentRequests(object sender, RoutedEventArgs e)
        {
            if(!currentRequestsPanel.IsPressed && !grid.Children.Contains(currentRequestsPanel))
            {               
                currentRequestsPanel.listRequests.Children.RemoveRange(1, currentRequestsPanel.listRequests.Children.Count - 1);
                if (requests.Count == 0)
                    currentRequestsPanel.NoRequest();
                else
                {
                    foreach (var r in requests)
                        currentRequestsPanel.AddNewRequest(r.Key, r.Value.Item1);

                    foreach (var item in currentRequestsPanel.listRequests.Children)
                        if (item is StackPanel panel)
                            panel.Children.OfType<Image>().First().MouseUp += CloseRequestBtn_MouseUp;

                    SetTimerInitialize();
                }
                Grid.SetRow(currentRequestsPanel, 1);
                Grid.SetColumn(currentRequestsPanel, 1);
                grid.Children.Add(currentRequestsPanel);
                ChangeButtonBackgroundColor((sender as Button).Name);
                currentRequestsPanel.IsPressed = true;
            }
            else if(currentRequestsPanel.IsPressed)
            {
                (sender as Button).Background = new SolidColorBrush(Color.FromRgb(182, 182, 182));
                currentRequestsPanel.IsPressed = false;
            }
        }

        private void SetTimerInitialize()
        {
            updateDiffRequestTime = new DispatcherTimer();
            updateDiffRequestTime.Interval = TimeSpan.FromSeconds(1);
            updateDiffRequestTime.Tick += new EventHandler(UpdateDiffRequestTime);
            updateDiffRequestTime.Start();

            updateStatisticsByReceivedMessage = new DispatcherTimer();
            updateStatisticsByReceivedMessage.Interval = TimeSpan.FromSeconds(25);
            updateStatisticsByReceivedMessage.Tick += new EventHandler(UpdateStatisticsByReceivedMessage);
            updateStatisticsByReceivedMessage.Start();
        }

        private void CloseRequestBtn_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Released)
            {
                var parent = (Panel)LogicalTreeHelper.GetParent(sender as Image);
                string phoneNumber = (parent.Children[0] as Label).Content.ToString();
                requests.Remove(phoneNumber);
                currentRequestsPanel.listRequests.Children.Remove(parent);
                if (requests.Count == 0)
                    currentRequestsPanel.NoRequest();
                programStatus.Text = $"Запрос по номеру {phoneNumber} отменён";
            }
        }

        private void UpdateDiffRequestTime(object sender, EventArgs e)
        {
            foreach (var r in requests)
                r.Value.Item1.OnPropertyChanged("DiffRequestTime");
        }

        private async void UpdateStatisticsByReceivedMessage(object sender, EventArgs e)
        {
            List<string> listPhoneNumbers = new List<string>();
            foreach (var r in requests)
            {
                r.Value.Item1.OnPropertyChanged("StatisticsByReceivedMessage");
                if (r.Value.Item1.ReceivedMessagesNumber == r.Value.Item1.MessagesNumber)
                    listPhoneNumbers.Add(r.Key);
            }

            if(listPhoneNumbers.Count > 0)
            {
                await Task.Delay(10000);
                await Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate () { foreach (var numb in listPhoneNumbers) StopReceivedData(numb); });
            }

            if (requests.Count == 0)
            {
                currentRequestsPanel.NoRequest();
                updateStatisticsByReceivedMessage.Stop();
                updateDiffRequestTime.Stop();
            }
        }

        private async void OpenFileOfMessages(object sender, RoutedEventArgs e)
        {
            operationProgress.Visibility = Visibility.Visible;
            dataLoading.Value = 0;
            
            await Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)async delegate()
            {
                IEnumerable<DriveInfo> drivesInfo = DriveInfo.GetDrives().Where(drive => drive.DriveType == DriveType.Removable);
                foreach (var drive in drivesInfo)
                {
                    if (drive.IsReady)
                    {
                        string filePath = Directory.GetFiles(string.Format(@"{0}", drive.Name), "375*").FirstOrDefault();
                        if (filePath != null)
                        {
                            FileInfo smsHistoryFile = new FileInfo(filePath);
                            double stepValue = await GetStepValueForProgressOperation(filePath);
                            using (StreamReader streamReader = smsHistoryFile.OpenText())
                            {
                                List<ShortMessage> listShortMessages = new List<ShortMessage>();
                                string sms, phoneNumber = $"+{smsHistoryFile.Name.Replace(".txt","")}";
                                while ((sms = streamReader.ReadLine()) != null)
                                {
                                    listShortMessages.Add(new ShortMessage(phoneNumber, sms));                                    
                                    dataLoading.Value += Convert.ToDouble(stepValue);
                                }
                                programStatus.Text = "Чтение файла выполнено.";
                                dataLoading.Value = 0;

                                await Task.Run(() => WriteSmsInDb(listShortMessages));
                                programStatus.Text = "Данные помещены в хранилище.";
                                dataLoading.Value = 0;

                                AddNewMessageToDataTable(listShortMessages);
                                programStatus.Text = "Файл успешно выгружен в таблицу.";

                                await Task.Delay(3000);
                                operationProgress.Visibility = Visibility.Hidden;
                            }
                        }
                    }
                }
            });            
        }

        private void AddMessagesFromDb(List<ShortMessage> messages)
        {
            foreach(var msg in messages)
            {
                messagesHistoryView.Items.Add(new ListViewItem()
                {
                    Content = new DataTableMessageRepresentation(msg.MessageNumber, msg.Sender, msg.ReceivedDateTime.Date, msg.ReceivedDateTime.ToString("HH:mm:ss"), msg.Message)
                });
            }
        }

        private void AddNewMessageToDataTable(List<ShortMessage> newMessages)
        {
            List<ShortMessage> oldMessages = new List<ShortMessage>();
            string messageBoxText = $"Запрос по номеру {newMessages[0].Sender} содержит ранее полученные сообщения: ";
            double stepValue = 100 / (double)newMessages.Count;
            foreach (var msg in newMessages)
            {
                var messages = new DataTableMessageRepresentation(msg.MessageNumber, msg.Sender, msg.ReceivedDateTime.Date, msg.ReceivedDateTime.ToString("HH:mm:ss"), msg.Message);

                int msgIndex = messagesHistoryView.Items.IndexOf(new ListViewItem() { Content = messages });
                if (msgIndex == -1)
                {
                    messagesHistoryView.Items.Add(new ListViewItem() { Content = messages });
                    (messagesHistoryView.Items[messagesHistoryView.Items.Count - 1] as ListViewItem).Background = new SolidColorBrush(Color.FromRgb(252, 65, 80));
                }
                else
                {
                    messageBoxText += msg.MessageNumber;
                    oldMessages.Add(msg);
                }
                dataLoading.Value += stepValue;
            }

            if (oldMessages.Count > 0)
                MessageBox.Show(messageBoxText, "Результат запроса");
        }

        private void WriteSmsInDb(List<ShortMessage> listShortMessages)
        {
            double stepValue = 100 / (double)listShortMessages.Count;
            using (SystemDbContext context = new SystemDbContext(ConfigurationManager.ConnectionStrings["BatteryMonitoringSystemDb"].ConnectionString))
            {
                foreach (var msg in listShortMessages)
                {
                    var query = context.Informations.Where(info => info.MessageNumber == msg.MessageNumber &&
                        info.InformationSource.InternationalCode + info.InformationSource.PhoneNumber == msg.Sender).ToList();

                    if (query.Count == 0)
                    {
                        var infoSource = context.InformationSources.Where(source => source.InternationalCode + source.PhoneNumber == msg.Sender).First();

                        context.Informations.Add(new Information
                        {
                            MessageNumber = msg.MessageNumber,
                            MessageDateTime = msg.ReceivedDateTime,
                            Message = msg.Message,
                            InformationSourceID = infoSource.InformationSourceID
                        });
                        context.SaveChanges();
                    }

                    Dispatcher.Invoke(() =>
                    {
                        dataLoading.Value += stepValue;
                    });
                }
            }
        }

        private void StopReceivedData(string byPhoneNumber)
        {
            //Remove request from dictionary
            requests[byPhoneNumber].Item2.Change(Timeout.Infinite, Timeout.Infinite);
            requests[byPhoneNumber].Item2.Dispose();
            requests.Remove(byPhoneNumber);

            //Remove element from CurrentRequestPanel
            if (currentRequestsPanel.IsPressed)
            {
                foreach (var child in currentRequestsPanel.listRequests.Children)
                {
                    if (child is StackPanel panel)
                    {
                        if (panel.Name == $"request{byPhoneNumber.TrimStart('+')}")
                        {
                            currentRequestsPanel.listRequests.Children.Remove(panel);
                            break;
                        }
                    }
                }
            }

            //Enabled comboBoxItem with this phoneNumber
            foreach (ComboBoxItem comboBoxItem in manualModePanel.choosePhoneNumber.Items)
            {
                if (comboBoxItem.Content.ToString() == byPhoneNumber)
                {
                    comboBoxItem.IsEnabled = true;
                    if (comboBoxItem == manualModePanel.choosePhoneNumber.SelectedItem)
                        manualModePanel.ChangeButtonsAvailability();
                }
            }

            programStatus.Text = $"Прием данных от {byPhoneNumber} завершён";
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
            if (choseInformationSource.Count > 0)
            {
                Task<Excel.Workbook> createExcelFileTask = new Task<Excel.Workbook>(() => CreateExcelFileAsync());
                createExcelFileTask.Start();
                
                var excelWorkbook = createExcelFileTask.Result;

                Dispatcher.BeginInvoke(DispatcherPriority.Normal, new DispatcherOperationCallback(delegate (object value)
                {                                          
                    for (int k = 0; k < choseInformationSource.Count; k++)
                    {
                        List<DataTableMessageRepresentation> listShortMessage = new List<DataTableMessageRepresentation>();
                        foreach (ListViewItem item in messagesHistoryView.Items)
                        {
                            var msg = (DataTableMessageRepresentation)item.Content;
                            if (msg.Sender == choseInformationSource[k])
                                listShortMessage.Add(msg);
                        }


                        var rowIndex = 1;
                        foreach (var row in listShortMessage)
                        {
                            rowIndex++;
                            excelApp.Worksheets[choseInformationSource[k]].Cells[rowIndex, "A"] = row.MessageNumber;
                            excelApp.Worksheets[choseInformationSource[k]].Cells[rowIndex, "B"] = row.ReceivedDate;
                            excelApp.Worksheets[choseInformationSource[k]].Cells[rowIndex, "C"] = row.ReceivedTime;
                            excelApp.Worksheets[choseInformationSource[k]].Cells[rowIndex, "D"] = row.Message;
                        }

                    }

                    excelWorkbook.SaveAs(Environment.CurrentDirectory + "\\" + DateTime.Now.ToString("dd-MM-yyyy H-mm-ss") + ".xlsx", Type.Missing, Type.Missing, Type.Missing, Type.Missing,
                        Type.Missing, Excel.XlSaveAsAccessMode.xlNoChange, Type.Missing, Type.Missing, Type.Missing, Type.Missing);

                    programStatus.Text = "Excel файл успешно создан!";
                    excelApp.Quit();
                    return null;
                }), excelWorkbook);
            }
        }

        private Excel.Workbook CreateExcelFileAsync()
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
            return excelWorkbook;
        }

        private void MessagesHistoryViewItem_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is ListViewItem item)
                item.Background = Brushes.White;
        }
    }
}
