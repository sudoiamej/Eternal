using System.Windows;
using Eternal.ViewModels;
using Eternal.Services.System;

namespace Eternal.Views
{
    public partial class HelpWindow : Window
    {
        private readonly HelpViewModel _viewModel;

        public HelpWindow(string? initialTopicId = null)
        {
            InitializeComponent();
            _viewModel = new HelpViewModel(new WindowsKnowledgeBaseService());
            this.DataContext = _viewModel;
            
            if (!string.IsNullOrEmpty(initialTopicId))
            {
                _ = _viewModel.InitializeAsync(initialTopicId);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
