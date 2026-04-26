using System.Windows;

namespace Eternal.Views.Helpers
{
    public partial class InputWindow : Window
    {
        public string Result { get; private set; } = string.Empty;

        public InputWindow(string prompt, string title = "Input", string defaultVal = "")
        {
            InitializeComponent();
            Title = title;
            PromptText.Text = prompt;
            InputBox.Text = defaultVal;
            InputBox.Focus();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            Result = InputBox.Text;
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        public static string? Show(string prompt, string title = "Input", string defaultVal = "")
        {
            var win = new InputWindow(prompt, title, defaultVal);
            win.Owner = System.Windows.Application.Current.MainWindow;
            if (win.ShowDialog() == true) return win.Result;
            return null;
        }
    }
}
