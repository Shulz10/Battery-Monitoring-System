using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace BatteryMonitoringSystem
{
    /// <summary>
    /// Interaction logic for AutoModePanel.xaml
    /// </summary>
    public partial class AutoModePanel : UserControl
    {
        public AutoModePanel()
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
            DependencyProperty.Register("IsPressed", typeof(bool), typeof(AutoModePanel),
                new PropertyMetadata(false, new PropertyChangedCallback(IsPresedChanged)));

        private static void IsPresedChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs args)
        {
            AutoModePanel panel = (AutoModePanel)depObj;
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

        private static void RemoveUserControl(AutoModePanel panel)
        {
            var parent = (Panel)LogicalTreeHelper.GetParent(panel);
            parent.Children.Remove(panel);
        }

        private void SetAutoModeBtn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void AutoModeParametersPanel_Loaded(object sender, RoutedEventArgs e)
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
    }
}
