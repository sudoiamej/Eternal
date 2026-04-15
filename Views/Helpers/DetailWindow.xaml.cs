using System.Collections.Generic;
using System.Windows;
using Eternal.Models;

namespace Eternal.Views.Helpers
{
    public partial class DetailWindow : Window
    {
        public DetailWindow(string title, string type, List<PropertyItem> properties)
        {
            InitializeComponent();
            Title = $"{title} Properties";
            TitleHeader.Text = title;
            TypeHeader.Text = type;
            PropertiesList.ItemsSource = properties;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
