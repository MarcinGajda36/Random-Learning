namespace Grains.GrainStorage;

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Configuration;
using Orleans.Runtime;
using Orleans.Storage;

public sealed class FileGrainStorageOptions : IStorageProviderSerializerOptions
{
    public required string RootDirectory { get; set; }

    public required IGrainStorageSerializer GrainStorageSerializer { get; set; }
}

public sealed class FileGrainStorage(
    string storageName,
    FileGrainStorageOptions storageOptions,
    IOptions<ClusterOptions> clusterOptions)
    : IGrainStorage, ILifecycleParticipant<ISiloLifecycle>, IDisposable
{
    private readonly ClusterOptions _clusterOptions = clusterOptions.Value;
    private IDisposable? siloLifecycleSubscription;

    public Task ClearStateAsync<T>(
        string stateName,
        GrainId grainId,
        IGrainState<T> grainState)
    {
        var fName = GetKeyString(stateName, grainId);
        var path = Path.Combine(storageOptions.RootDirectory, fName);
        var fileInfo = new FileInfo(path);
        if (fileInfo.Exists)
        {
            if (fileInfo.LastWriteTimeUtc.ToString() != grainState.ETag)
            {
                throw new InconsistentStateException(
                    $"""
                    Version conflict (ClearState): ServiceId={_clusterOptions.ServiceId}
                    ProviderName={storageName} GrainType={typeof(T)}
                    GrainReference={grainId}.
                    """);
            }

            grainState.ETag = null;
            grainState.State = (T)Activator.CreateInstance(typeof(T))!;

            fileInfo.Delete();
        }

        return Task.CompletedTask;
    }

    public async Task ReadStateAsync<T>(
        string stateName,
        GrainId grainId,
        IGrainState<T> grainState)
    {
        var fName = GetKeyString(stateName, grainId);
        var path = Path.Combine(storageOptions.RootDirectory, fName);
        var fileInfo = new FileInfo(path);
        if (fileInfo is { Exists: false })
        {
            grainState.State = (T)Activator.CreateInstance(typeof(T))!;
            return;
        }

        using var stream = fileInfo.OpenText();
        var storedData = await stream.ReadToEndAsync();

        grainState.State = storageOptions.GrainStorageSerializer.Deserialize<T>(new BinaryData(storedData));
        grainState.ETag = fileInfo.LastWriteTimeUtc.ToString();
    }

    public async Task WriteStateAsync<T>(
        string stateName,
        GrainId grainId,
        IGrainState<T> grainState)
    {
        var storedData = storageOptions.GrainStorageSerializer.Serialize(grainState.State);
        var fName = GetKeyString(stateName, grainId);
        var path = Path.Combine(storageOptions.RootDirectory, fName);
        var fileInfo = new FileInfo(path);
        if (fileInfo.Exists && fileInfo.LastWriteTimeUtc.ToString() != grainState.ETag)
        {
            throw new InconsistentStateException(
                $"""
                Version conflict (WriteState): ServiceId={_clusterOptions.ServiceId}
                ProviderName={storageName} GrainType={typeof(T)}
                GrainReference={grainId}.
                """);
        }

        await File.WriteAllBytesAsync(path, storedData.ToArray());

        fileInfo.Refresh();
        grainState.ETag = fileInfo.LastWriteTimeUtc.ToString();
    }

    public void Participate(ISiloLifecycle lifecycle)
    {
        siloLifecycleSubscription = lifecycle.Subscribe(
            observerName: OptionFormattingUtilities.Name<FileGrainStorage>(storageName),
            stage: ServiceLifecycleStage.ApplicationServices,
            onStart: token =>
            {
                Directory.CreateDirectory(storageOptions.RootDirectory);
                return Task.CompletedTask;
            });
    }

    private string GetKeyString(string grainType, GrainId grainId)
        => $"{_clusterOptions.ServiceId}.{grainId.Key}.{grainType}";

    public void Dispose()
        => siloLifecycleSubscription?.Dispose();
}