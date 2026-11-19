using System.Threading;
using System.Threading.Tasks;

namespace Prison.Core
{
    public interface IWorldStorage
    {
        string StorageName { get; }
        Task SaveAsync(WorldDto world, string destination, CancellationToken token = default);
        Task<WorldDto> LoadAsync(string source, CancellationToken token = default);
    }
}