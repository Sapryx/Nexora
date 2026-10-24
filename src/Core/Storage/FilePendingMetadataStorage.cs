using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Core.Logging;
using Microsoft.Extensions.Logging;

namespace Core.Storage;

public class FilePendingMetadataStorage : IPendingMetadataStorage
{
    private const string Extension = ".json";
    private readonly string directory;
    private readonly ILogger logger;

    public FilePendingMetadataStorage(string directory, ILogger<FilePendingMetadataStorage> logger)
    {
        this.directory = directory;
        this.logger = logger;
    }

    public List<MetadataChange> LoadAll()
    {
        if(!Directory.Exists(directory))
        {
            return [];
        }

        var changes = new List<MetadataChange>();

        foreach(string filePath in Directory.EnumerateFiles(directory, $"*{Extension}"))
        {
            try
            {
                string json = File.ReadAllText(filePath);
                var change = JsonSerializer.Deserialize(json, MetadataChangeJsonContext.Default.MetadataChange);

                if(change != null)
                {
                    changes.Add(change);
                }
            }
            catch(Exception ex)
            {
                logger.Warn(ex, $"Ignored unreadable pending metadata change {filePath}");
            }
        }

        return changes;
    }

    public void Save(MetadataChange change)
    {
        Directory.CreateDirectory(directory);
        string filePath = GetFilePath(change.AudioPath);
        string tempPath = $"{filePath}.tmp";

        // Written next to the target and moved over it, so a crash never leaves a half-written change.
        File.WriteAllText(tempPath, JsonSerializer.Serialize(change, MetadataChangeJsonContext.Default.MetadataChange));
        File.Move(tempPath, filePath, true);
    }

    public void Delete(string audioPath)
    {
        string filePath = GetFilePath(audioPath);

        if(File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }

    // An audio path can't be used as a file name, so the change file is named after its SHA-256 hash.
    private string GetFilePath(string audioPath)
    {
        string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(audioPath)));
        return Path.Combine(directory, $"{hash}{Extension}");
    }
}
