using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace face_finder.Shared.Interfaces
{
    public interface IPersonService
    {
        Task<string> GetFullNameAsync(string hashName, CancellationToken cancellationToken = default);

        Task<string> GetDetailsAsync(string hashName, CancellationToken cancellationToken = default);

        Task<string> GetFaceAsync(string hashName, CancellationToken cancellationToken = default);

        Task<Stream> GetFaceStreamAsync(string hashName, CancellationToken cancellationToken = default);
    }
}