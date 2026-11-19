using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Prison.Core
{
    public class JsonWorldStorage : IWorldStorage
    {
        public string StorageName => "JSON Storage (System.Text.Json)";

        private static readonly JsonSerializerOptions Options = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        public async Task SaveAsync(WorldDto world, string path, CancellationToken token = default)
        {
            string json = JsonSerializer.Serialize(world, Options);
            await File.WriteAllTextAsync(path, json, Encoding.UTF8, token);
        }

        public async Task<WorldDto> LoadAsync(string path, CancellationToken token = default)
        {
            string json = await File.ReadAllTextAsync(path, Encoding.UTF8, token);
            if (string.IsNullOrWhiteSpace(json))
                throw new InvalidDataException("JSON file is empty.");

            var world = JsonSerializer.Deserialize<WorldDto>(json, Options)
                ?? throw new InvalidDataException("Failed to parse JSON structure.");

            WorldValidator.Validate(world);
            return world;
        }
    }
}
