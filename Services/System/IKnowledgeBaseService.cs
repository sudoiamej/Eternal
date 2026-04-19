using System.Collections.Generic;
using System.Threading.Tasks;
using Eternal.Models;

namespace Eternal.Services.System
{
    public interface IKnowledgeBaseService
    {
        Task<List<HelpArticle>> GetAllArticlesAsync();
        Task<List<HelpArticle>> SearchArticlesAsync(string query);
    }
}
