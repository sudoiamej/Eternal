namespace Eternal.ViewModels
{
    /// <summary>
    /// Interface for ViewModels that can release memory-intensive caches or data structures.
    /// </summary>
    public interface IMemoryOptimizable
    {
        void ReleaseMemory();
    }
}
