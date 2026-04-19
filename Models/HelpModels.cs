using System.Collections.Generic;

namespace Eternal.Models
{
    public class HelpArticle
    {
        public string Id { get; set; } = string.Empty;
        public string Category { get; set; } = "General";
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Icon { get; set; } = "Book";
    }
}
