using System.Collections.Specialized;
using System.Windows.Controls;

namespace Eternal.Views.Modules
{
    public partial class ConsoleView : System.Windows.Controls.UserControl
    {
        public ConsoleView()
        {
            InitializeComponent();
            
            // Auto-scroll logic: Listen to the collection changes
            if (ConsoleOutputList.ItemsSource is INotifyCollectionChanged observable)
            {
                observable.CollectionChanged += (s, e) =>
                {
                    if (e.Action == NotifyCollectionChangedAction.Add)
                    {
                        // Scroll to the last item added
                        if (ConsoleOutputList.Items.Count > 0)
                        {
                            ConsoleOutputList.ScrollIntoView(ConsoleOutputList.Items[ConsoleOutputList.Items.Count - 1]);
                        }
                    }
                };
            }
        }
    }
}
