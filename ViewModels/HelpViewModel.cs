using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Eternal.Models;
using Eternal.Services.System;

namespace Eternal.ViewModels
{
    public partial class HelpViewModel : ObservableObject
    {
        private readonly IKnowledgeBaseService _kbService;
        private List<HelpArticle> _allArticles = new();

        [ObservableProperty] private string _searchQuery = string.Empty;
        [ObservableProperty] private HelpArticle? _selectedArticle;
        [ObservableProperty] private bool _isLoading;

        public ObservableCollection<HelpArticle> FilteredArticles { get; } = new();

        public HelpViewModel(IKnowledgeBaseService kbService)
        {
            _kbService = kbService;
            _ = InitializeAsync();
        }

        public async Task InitializeAsync(string? initialTopicId = null)
        {
            IsLoading = true;
            try
            {
                _allArticles = await _kbService.GetAllArticlesAsync();
                ApplyFilter();

                if (!string.IsNullOrEmpty(initialTopicId))
                {
                    SelectedArticle = _allArticles.FirstOrDefault(a => a.Id.Equals(initialTopicId, StringComparison.OrdinalIgnoreCase));
                }
                
                if (SelectedArticle == null && FilteredArticles.Any())
                {
                    SelectedArticle = FilteredArticles.First();
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        partial void OnSearchQueryChanged(string value)
        {
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            var query = SearchQuery?.Trim();
            var filtered = string.IsNullOrWhiteSpace(query) 
                ? _allArticles 
                : _allArticles.Where(a => 
                    a.Title.Contains(query, StringComparison.OrdinalIgnoreCase) || 
                    a.Category.Contains(query, StringComparison.OrdinalIgnoreCase)
                ).ToList();

            FilteredArticles.Clear();
            foreach (var article in filtered)
            {
                FilteredArticles.Add(article);
            }
        }
    }
}
