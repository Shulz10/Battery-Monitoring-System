using System;
using BatteryMonitoringSystem.Models;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace BatteryMonitoringSystem
{
    /// <summary>
    /// Interaction logic for UserControl.xaml
    /// </summary>
    public partial class InformationSourcePanel : UserControl
    {
        ListView MessageTable;
        public InformationSourcePanel(ListView messageTable)
        {
            InitializeComponent();

            MessageTable = messageTable;
        }

        public bool IsPressed
        {
            get { return (bool)GetValue(IsPressedProperty); }
            set { SetValue(IsPressedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsPressed.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsPressedProperty =
            DependencyProperty.Register("IsPressed", typeof(bool), typeof(InformationSourcePanel),
                new PropertyMetadata(false, new PropertyChangedCallback(IsPressedChanged)));

        private static void IsPressedChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs args)
        {
            InformationSourcePanel panel = (InformationSourcePanel)depObj;
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

        private static void RemoveUserControl(InformationSourcePanel panel)
        {
            var parent = (Panel)LogicalTreeHelper.GetParent(panel);
            parent.Children.Remove(panel);
        }

        private void InformationSourcesPanel_Loaded(object sender, RoutedEventArgs e)
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

        private void AddSourceBtn_Click(object sender, RoutedEventArgs e)
        {
            NewPhoneWindow newPhoneWindow = new NewPhoneWindow
            {
                Owner = Window.GetWindow(this)
            };
            if (newPhoneWindow.ShowDialog() == true)
            {
                using (SystemDbContext context = new SystemDbContext(ConfigurationManager.ConnectionStrings["BatteryMonitoringSystemDb"].ConnectionString))
                {
                    var infoSource = (from sources in context.InformationSources select new { sources.InternationalCode, sources.PhoneNumber }).ToList().Last();

                    CheckBox checkBox = new CheckBox()
                    {
                        FlowDirection = FlowDirection.RightToLeft,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 5, 0, 5),
                        FontSize = 14,
                        Content = new TextBlock()
                        {
                            FlowDirection = FlowDirection.LeftToRight,
                            Text = infoSource.InternationalCode + infoSource.PhoneNumber,
                            Margin = new Thickness(100, 0, 0, 0)
                        }
                    };

                    sourcePanel.Children.Add(checkBox);
                }
            }
        }

        //private void ChooseSourceBtn_Click(object sender, RoutedEventArgs e)
        //{
        //    List<string> choseInformationSource = new List<string>();
        //    using (SystemDbContext context = new SystemDbContext(ConfigurationManager.ConnectionStrings["BatteryMonitoringSystemDb"].ConnectionString))
        //    {
        //        foreach (var source in informationSourcePanel.Children.OfType<CheckBox>())
        //        {
        //            if (source.IsChecked ?? false)
        //                choseInformationSource.Add((source.Content as TextBlock).Text);
        //        }
        //        foreach (var infoSource in choseInformationSource)
        //        {
        //            var query = (from source in context.InformationSources
        //                            join info in context.Informations on source.InformationSourceID equals info.InformationSourceID
        //                            where source.InternationalCode == infoSource.Substring(0, 6) && source.PhoneNumber == infoSource.Substring(6)
        //                            select new
        //                            {
        //                                MessageNumber = info.MessageNumber,
        //                                MessageDateTime = info.MessageDateTime,
        //                                Message = info.Message,
        //                                PhoneNumber = info.InformationSource.InternationalCode + info.InformationSource.PhoneNumber
        //                            }).ToList();
        //            //MessageTable.Items.Add(query);
        //            MessageTable.ItemsSource = query;
        //        }
        //    }
        //}

        private void RemoveSourceBtn_Click(object sender, RoutedEventArgs e)
        {
            using (SystemDbContext context = new SystemDbContext(ConfigurationManager.ConnectionStrings["BatteryMonitoringSystemDb"].ConnectionString))
            {
                List<CheckBox> checkBoxes = new List<CheckBox>();
                foreach (var source in sourcePanel.Children.OfType<CheckBox>())
                {
                    if (source.IsChecked ?? false)
                    {
                        string phone = (source.Content as TextBlock).Text;
                        var query = from s in context.InformationSources
                                    where s.InternationalCode == phone.Substring(0, 6)
                                    && s.PhoneNumber == phone.Substring(6)
                                    select s;
                        context.InformationSources.RemoveRange(query);
                        context.SaveChanges();
                        checkBoxes.Add(source);                       
                    }
                }

                foreach (var checkBox in checkBoxes)
                    sourcePanel.Children.Remove(checkBox);
            }
        }

        private void InformationSourcePanel_Initialized(object sender, EventArgs e)
        {
            using (SystemDbContext context = new SystemDbContext(ConfigurationManager.ConnectionStrings["BatteryMonitoringSystemDb"].ConnectionString))
            {
                var phoneNumbers = context.InformationSources.Select(c => new { InternationalCode = c.InternationalCode, PhoneNumber = c.PhoneNumber }).ToList();

                if (phoneNumbers.Count > 0)
                {
                    foreach (var phone in phoneNumbers)
                    {
                        CheckBox checkBox = new CheckBox()
                        {
                            FlowDirection = FlowDirection.RightToLeft,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Margin = new Thickness(0, 5, 0, 5),
                            FontSize = 14,
                            Content = new TextBlock()
                            {
                                FlowDirection = FlowDirection.LeftToRight,
                                Text = phone.InternationalCode + phone.PhoneNumber,                                
                                Margin = new Thickness(100, 0, 0, 0)
                            }
                        };

                        sourcePanel.Children.Add(checkBox);
                    }
                }
                else
                {
                    Label label = new Label()
                    {
                        Content = "Список источников информации пуст!",
                        FontFamily = new FontFamily("Sitka Display"),
                        FontStyle = FontStyles.Italic,
                        FontSize = 16,
                        Foreground = Brushes.Red,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 5, 0, 5)
                    };

                    sourcePanel.Children.Add(label);
                }
            }
        }        
    }
}
