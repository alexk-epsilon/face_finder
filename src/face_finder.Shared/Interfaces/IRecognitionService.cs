using System.Threading;
using System.Threading.Tasks;
using OtYaBatka.Shared.Models;

namespace OtYaBatka.Shared.Interfaces
{
    public interface IRecognitionService
    {
        Task<RecognitionResult> ProcessImageAsync(string base64Image, CancellationToken cancellationToken = default);
    }
}