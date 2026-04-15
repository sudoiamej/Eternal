using System.Windows;
using System.Windows.Controls;
using Eternal.ViewModels;

namespace Eternal.Views
{
    public partial class HelpWindow : Window
    {
        private readonly HelpViewModel _viewModel;

        public HelpWindow()
        {
            InitializeComponent();
            _viewModel = new HelpViewModel();
            this.DataContext = _viewModel;
        }

        private void TopicList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is ListBoxItem item)
            {
                _viewModel?.ChangeTopic(item.Content.ToString());
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}