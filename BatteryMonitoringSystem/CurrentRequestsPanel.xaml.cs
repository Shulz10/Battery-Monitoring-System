using BatteryMonitoringSystem.Models;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace BatteryMonitoringSystem
{
    /// <summary>
    /// Interaction logic for CurrentRequestsPanel.xaml
    /// </summary>
    public partial class CurrentRequestsPanel : UserControl
    {
        private DispatcherTimer[] timers;

        public CurrentRequestsPanel()
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
            DependencyProperty.Register("IsPressed", typeof(bool), typeof(CurrentRequestsPanel),
                new PropertyMetadata(false, new PropertyChangedCallback(IsPressedChanged)));

        private static void IsPressedChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs args)
        {
            CurrentRequestsPanel panel = (CurrentRequestsPanel)depObj;
            if(!panel.IsPressed && panel.Parent != null)
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

        private static void RemoveUserControl(CurrentRequestsPanel panel)
        {
            var parent = (Panel)LogicalTreeHelper.GetParent(panel);
            parent.Children.Remove(panel);
        }

        private void CurrentRequestsListPanel_Loaded(object sender, RoutedEventArgs e)
        {
            ThicknessAnimation animate = new ThicknessAnimation()
            {
                From = new Thickness(-700, 0, 0, 0),
                To = new Thickness(0, 0, 0, 0),
                Duration = TimeSpan.FromSeconds(1)
            };
            animate.Completed += (o, s) => { RemoveOldPanel(); };
            BeginAnimation(MarginProperty, animate);

            timers = new DispatcherTimer[5];
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

        private void AddNewRequest(string phoneNumber, SmsRequest smsRequest)
        {
            int index = Array.IndexOf(timers, null);
            if (index != -1)
            {
                timers[index] = new DispatcherTimer();
                timers[index].Interval = TimeSpan.FromSeconds(1);
                timers[index].Tick += DispatcherTimer_Tick;
            }
            RowDefinition newGridRow = new RowDefinition();
            listRequests.RowDefinitions.Add(newGridRow);

            Label phoneNumberLabel = new Label() { Content = phoneNumber };
            Grid.SetRow(phoneNumberLabel, listRequests.RowDefinitions.Count - 1);
            Grid.SetColumn(phoneNumberLabel, 0);

            Label messagesCounterLabel = new Label() { Content = $"{smsRequest.ReceivedMessagesNumber}/{smsRequest.MessagesNumber}" };
            Grid.SetRow(messagesCounterLabel, listRequests.RowDefinitions.Count - 1);
            Grid.SetColumn(messagesCounterLabel, 1);

            Label requestTimeLabel = new Label() { Content = $"{smsRequest.DiffRequestTime.ToString(@"hh\\:mm\\:ss")}" };
            Grid.SetRow(requestTimeLabel, listRequests.RowDefinitions.Count - 1);
            Grid.SetColumn(requestTimeLabel, 2);

            Button closeRequestBtn = new Button() { Content = "Close" };
            Grid.SetRow(closeRequestBtn, listRequests.RowDefinitions.Count - 1);
            Grid.SetColumn(closeRequestBtn, 3);

            listRequests.Children.Add(phoneNumberLabel);
            listRequests.Children.Add(messagesCounterLabel);
            listRequests.Children.Add(requestTimeLabel);
            listRequests.Children.Add(closeRequestBtn);
        }

        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            
        }
    }
}
