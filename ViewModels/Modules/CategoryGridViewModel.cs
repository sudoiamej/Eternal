using System;
using System.Collections.ObjectModel;
using Eternal.ViewModels;
using Eternal.Models;

namespace Eternal.ViewModels.Modules
{
    public partial class CategoryGridViewModel : BaseViewModel
    {
        public ObservableCollection<NavigationItem> Items { get; set; } = new ObservableCollection<NavigationItem>();

        public void Initialize(string title, ObservableCollection<NavigationItem> items)
        {
            Title = title;
            Items.Clear();
            if (items != null)
            {
                foreach (var item in items)
                {
                    Items.Add(item);
                }
            }
        }
    }
}
