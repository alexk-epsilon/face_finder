using System.Threading;
using System.Threading.Tasks;
using face_finder.Shared.Models;

namespace face_finder.Shared.Interfaces
{
    public interface IRecognitionService
    {
        Task<RecognitionResult> ProcessImageAsync(string base64Image, CancellationToken cancellationToken = default);
    }
}