using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Prison.Core
{
    public class XmlWorldStorage : IWorldStorage
    {
        public string StorageName => "XML Storage (XmlSerializer)";

        public Task SaveAsync(WorldDto world, string path, CancellationToken token = default)
        {
            return Task.Run(() =>
            {
                token.ThrowIfCancellationRequested();
                var serializer = new XmlSerializer(typeof(WorldDto));
                using var stream = File.Create(path);
                serializer.Serialize(stream, world);
            }, token);
        }

        public Task<WorldDto> LoadAsync(string path, CancellationToken token = default)
        {
            return Task.Run(() =>
            {
                token.ThrowIfCancellationRequested();
                var serializer = new XmlSerializer(typeof(WorldDto));
                using var stream = File.OpenRead(path);

                if (stream.Length == 0)
                    throw new InvalidDataException("XML file is empty.");

                var world = serializer.Deserialize(stream) as WorldDto
                    ?? throw new InvalidDataException("Invalid XML root element.");

                WorldValidator.Validate(world);
                return world;
            }, token);
        }
    }
}
