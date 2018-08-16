using BatteryMonitoringSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace BatteryMonitoringSystem
{
    /// <summary>
    /// Interaction logic for CurrentRequestsPanel.xaml
    /// </summary>
    public partial class CurrentRequestsPanel : UserControl
    {
        static ThicknessAnimation animate;
        public static AnimationClock animationClock { get; set; }

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

        public void AddNewRequest(string phoneNumber, SmsRequest smsRequest)
        {
            Label phoneNumberLabel = new Label() { Content = phoneNumber, Width = 120 };

            Label messagesCounterLabel = new Label() { Width = 40 };
            Binding binding = new Binding("StatisticsByReceivedMessage")
            {
                Source = smsRequest,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            BindingOperations.SetBinding(messagesCounterLabel, ContentProperty, binding);

            Label requestTimeLabel = new Label() { Width = 120 };
            binding = new Binding("DiffRequestTime")
            {
                Source = smsRequest,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            BindingOperations.SetBinding(requestTimeLabel, ContentProperty, binding);

            Image closeRequestBtn = new Image() { Source = new BitmapImage(new Uri("pack://application:,,,/Resources/closeBtn.png")) };

            StackPanel panel = new StackPanel() { Name = $"request{phoneNumber.TrimStart('+')}", Orientation = Orientation.Horizontal, Margin = new Thickness(10, 10, 0, 5) };
            panel.Children.Add(phoneNumberLabel);
            panel.Children.Add(messagesCounterLabel);
            panel.Children.Add(requestTimeLabel);
            panel.Children.Add(closeRequestBtn);

            DockPanel.SetDock(panel, Dock.Top);
            listRequests.Children.Add(panel);
        }

        public void NoRequest()
        {
            Label label = new Label()
            {
                Name = "RequestsCount",
                Content = "Список запросов пуст!",
                FontFamily = new FontFamily("Sitka Display"),
                FontStyle = FontStyles.Italic,
                FontSize = 16,
                Foreground = Brushes.Red,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 5, 0, 5)
            };

            DockPanel.SetDock(label, Dock.Top);
            if(listRequests.Children.Count == 0)
                listRequests.Children.Add(label);
        }
    }
}
