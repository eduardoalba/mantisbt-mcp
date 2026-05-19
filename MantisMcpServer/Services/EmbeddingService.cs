using OpenAI.Embeddings;
using Microsoft.Extensions.Logging;

namespace MantisMcpServer.Services
{
    public interface IEmbeddingService
    {
        Task<float[]> GetEmbeddingAsync(string text);
    }

    public class OpenAIEmbeddingService : IEmbeddingService
    {
        private readonly EmbeddingClient _client;
        private readonly ILogger<OpenAIEmbeddingService> _logger;

        public OpenAIEmbeddingService(string apiKey, string model, ILogger<OpenAIEmbeddingService> logger)
        {
            _logger = logger;
            _client = new EmbeddingClient(model, apiKey);
        }

        public async Task<float[]> GetEmbeddingAsync(string text)
        {
            try
            {
                var response = await _client.GenerateEmbeddingAsync(text);
                return response.Value.ToFloats().ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating embedding with OpenAI");
                throw;
            }
        }
    }
}
