using Edelstein.Assets.Management;
using Edelstein.Models.Manifest.Bundle;
using Edelstein.Models.Manifest.Movie;
using Edelstein.Models.Manifest.Sound;
using Edelstein.Models.Protocol;
using Edelstein.Protocol;

using Spectre.Console;

using System.CommandLine;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

// Bundles: /{platform}/{hash}/{name}.unity3d
// Movies: /{platform}/{hash}/{name}.usm.(ppart|spart)
// Sounds: /{platform}/{hash}/{name}.(acb|awb).(ppart|spart)

namespace Edelstein.Assets.Downloader;

internal class Program
{
    private static readonly JsonSerializerOptions SnakeCaseJsonSerializerOptions =
        new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    private static readonly JsonSerializerOptions IndentedJsonSerializerOptions =
        new() { WriteIndented = true };

    public static async Task<int> Main(string[] args)
    {
        Option<string> assetsHostOption = new(["--host", "-h"],
            () => "lovelive-schoolidolfestival2-assets.akamaized.net",
            "Host of asset storage");
        Option<string> extractedManifestsPathOption = new(["--extracted-manifests-path", "-m"],
            () => "manifests",
            "Path where extracted manifest files (Bundle.bytes, Movie.bytes and Sound.bytes) will be located");
        Option<string> downloadPathOption = new(["--download-path", "-d"],
            () => "data",
            "Path to which assets should be downloaded");
        Option<int> parallelDownloadsCountOption = new(["--parallel-downloads-count", "-p"],
            () => 10,
            "Count of parallel downloads");
        Option<bool> noAndroidOption = new("--no-android",
            () => false,
            "Exclude Android assets from download");
        Option<bool> noIosOption = new("--no-ios",
            () => false,
            "Exclude iOS assets from download");
        Option<bool> noJsonManifestOption = new("--no-manifest-json",
            () => false,
            "Exclude generating JSON from manifests");

        RootCommand rootCommand = new("Edelstein asset downloader");

        rootCommand.AddOption(assetsHostOption);
        rootCommand.AddOption(extractedManifestsPathOption);
        rootCommand.AddOption(downloadPathOption);
        rootCommand.AddOption(parallelDownloadsCountOption);
        rootCommand.AddOption(noAndroidOption);
        rootCommand.AddOption(noIosOption);
        rootCommand.AddOption(noJsonManifestOption);

        rootCommand.SetHandler(HandleRootCommandAsync, assetsHostOption, extractedManifestsPathOption, downloadPathOption,
            parallelDownloadsCountOption, noAndroidOption, noIosOption, noJsonManifestOption);

        return await rootCommand.InvokeAsync(args);
    }

    private static async Task HandleRootCommandAsync(string assetsHost, string extractedManifestsPath, string downloadPath,
        int parallelDownloadsCount, bool noAndroid, bool noIos, bool noJsonManifest)
    {
        ServicePointManager.DefaultConnectionLimit = parallelDownloadsCount;

        Directory.CreateDirectory(downloadPath);
        Directory.CreateDirectory(extractedManifestsPath);
        Directory.CreateDirectory(Path.Combine(extractedManifestsPath, AssetPlatformConverter.ToString(AssetPlatform.Android)));
        Directory.CreateDirectory(Path.Combine(extractedManifestsPath, AssetPlatformConverter.ToString(AssetPlatform.Ios)));

        HttpClient httpClient = new();

        AnsiConsole.WriteLine("Retrieving manifest hash...");

        string? androidManifestHash = null,
                iosManifestHash = null;

        if (!noAndroid)
            androidManifestHash = await RetrieveManifestHashAsync(httpClient, AssetPlatform.Android);
        if (!noIos)
            iosManifestHash = await RetrieveManifestHashAsync(httpClient, AssetPlatform.Ios);

        await using (StreamWriter sw = new(Path.Combine(extractedManifestsPath, "hashes.txt"),
            new FileStreamOptions
            {
                Mode = FileMode.Create,
                Access = FileAccess.Write,
                Share = FileShare.Read
            }))
        {
            if (!noAndroid)
            {
                AnsiConsole.MarkupLine($"Android manifest hash is [green]{androidManifestHash}[/]");
                await sw.WriteLineAsync($"Android manifest hash is {androidManifestHash}");
            }

            if (!noIos)
            {
                AnsiConsole.MarkupLine($"iOS manifest hash is [green]{iosManifestHash}[/]");
                await sw.WriteLineAsync($"iOS manifest hash is {iosManifestHash}");
            }
        }

        AnsiConsole.WriteLine("Downloading manifest...");

        if (!noAndroid)
            await DownloadManifestAsync(httpClient, assetsHost, AssetPlatform.Android, androidManifestHash!, downloadPath);
        if (!noIos)
            await DownloadManifestAsync(httpClient, assetsHost, AssetPlatform.Ios, iosManifestHash!, downloadPath);

        AnsiConsole.WriteLine("Please extract manifest data to Bundle.bytes, Movie.bytes and Sound.bytes.");
        AnsiConsole.WriteLine("Continue after extraction.");

        while (!AnsiConsole.Confirm("Continue?")) { }

        if (!noAndroid)
        {
            AnsiConsole.WriteLine("Decrypting and deserializing Android manifests...");
            (BundleManifest bundles, MovieManifest movies, SoundManifest sounds) =
                await DeserializeManifestsAsync(extractedManifestsPath, AssetPlatform.Android);

            if (!noJsonManifest)
            {
                AnsiConsole.WriteLine("Serializing Android manifests to JSON...");
                await SerializeManifestsToJsonAsync(extractedManifestsPath, AssetPlatform.Android, bundles, movies, sounds);
            }

            AnsiConsole.WriteLine("Starting Android assets download...");
            await DownloadAssetsAsync(httpClient, assetsHost, AssetPlatform.Android, downloadPath, bundles,
                movies, sounds, parallelDownloadsCount);
        }

        if (!noIos)
        {
            AnsiConsole.WriteLine("Decrypting and deserializing iOS manifests...");
            (BundleManifest bundles, MovieManifest movies, SoundManifest sounds) =
                await DeserializeManifestsAsync(extractedManifestsPath, AssetPlatform.Ios);

            if (!noJsonManifest)
            {
                AnsiConsole.WriteLine("Serializing iOS manifests to JSON...");
                await SerializeManifestsToJsonAsync(extractedManifestsPath, AssetPlatform.Ios, bundles, movies, sounds);
            }

            AnsiConsole.WriteLine("Starting iOS assets download...");
            await DownloadAssetsAsync(httpClient, assetsHost, AssetPlatform.Ios, downloadPath, bundles,
                movies, sounds, parallelDownloadsCount);
        }

        AnsiConsole.WriteLine("Download completed!");

        Console.ReadKey();
    }

    private static async Task<string> RetrieveManifestHashAsync(HttpClient httpClient, AssetPlatform platform)
    {
        string platformString = AssetPlatformConverter.ToPlayerString(platform);

        string clientData = ProtocolCryptor.Encrypt(JsonSerializer.Serialize(new
        {
            asset_version = "0",
            environment = "release"
        }));

        using HttpRequestMessage httpRequestMessage = new(HttpMethod.Post, "https://api.app.lovelive-sif2.bushimo.jp/api/start/assetHash");

        httpRequestMessage.Headers.Add("Aoharu-Platform", platformString);

        httpRequestMessage.Content = new StringContent(clientData, MediaTypeHeaderValue.Parse("application/json"));

        using HttpResponseMessage response = await httpClient.SendAsync(httpRequestMessage);

        string decryptedData = ProtocolCryptor.Decrypt(await response.Content.ReadAsStringAsync());

        return JsonSerializer.Deserialize<ServerResponse<AssetHashResponse>>(decryptedData, SnakeCaseJsonSerializerOptions)!.Data.AssetHash;
    }

    private static async Task DownloadManifestAsync(HttpClient httpClient, string assetsHost, AssetPlatform platform, string manifestHash,
        string downloadPath)
    {
        string platformString = AssetPlatformConverter.ToString(platform);

        const string manifestName = "387b0126300c54515911bffb6540982d";

        Uri downloadUri = new($"https://{assetsHost}/{platformString}/{manifestHash}/{manifestName}.unity3d");

        await using Stream httpStream = await httpClient.GetStreamAsync(downloadUri);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(downloadPath, downloadUri.AbsolutePath[1..]))!);
        await using StreamWriter fileWriter = new(Path.Combine(downloadPath, downloadUri.AbsolutePath[1..]), false);
        await httpStream.CopyToAsync(fileWriter.BaseStream);
    }

    private static async Task<(BundleManifest Bundles, MovieManifest Movies, SoundManifest Sounds)> DeserializeManifestsAsync(
        string extractedManifestsPath, AssetPlatform platform)
    {
        string platformString = AssetPlatformConverter.ToString(platform);

        BundleManifest bundles;
        MovieManifest movies;
        SoundManifest sounds;

        await using (FileStream bundlesFileStream = new(Path.Combine(extractedManifestsPath, platformString, "Bundle.bytes"),
            FileMode.Open, FileAccess.Read))
        {
            bundles = ManifestSerializer.DeserializeCompressedBinary<BundleManifest, BundleTypesSerializationBinder>(
                ManifestCryptor.Decrypt(bundlesFileStream));
        }

        await using (FileStream moviesFileStream = new(Path.Combine(extractedManifestsPath, platformString, "Movie.bytes"),
            FileMode.Open, FileAccess.Read))
        {
            movies = ManifestSerializer.DeserializeCompressedBinary<MovieManifest, MovieTypesSerializationBinder>(
                ManifestCryptor.Decrypt(moviesFileStream));
        }

        await using (FileStream soundsFileStream = new(Path.Combine(extractedManifestsPath, platformString, "Sound.bytes"),
            FileMode.Open, FileAccess.Read))
        {
            sounds = ManifestSerializer.DeserializeCompressedBinary<SoundManifest, SoundTypesSerializationBinder>(
                ManifestCryptor.Decrypt(soundsFileStream));
        }

        return (bundles, movies, sounds);
    }

    private static async Task SerializeManifestsToJsonAsync(string extractedManifestsPath, AssetPlatform platform, BundleManifest bundles,
        MovieManifest movies, SoundManifest sounds)
    {
        string platformString = AssetPlatformConverter.ToString(platform);

        await using (StreamWriter sw = new(Path.Combine(extractedManifestsPath, $"{platformString}Bundle.json"), false))
        {
            await sw.WriteAsync(JsonSerializer.Serialize(bundles, IndentedJsonSerializerOptions));
        }

        await using (StreamWriter sw = new(Path.Combine(extractedManifestsPath, $"{platformString}Movie.json"), false))
        {
            await sw.WriteAsync(JsonSerializer.Serialize(movies, IndentedJsonSerializerOptions));
        }

        await using (StreamWriter sw = new(Path.Combine(extractedManifestsPath, $"{platformString}Sound.json"), false))
        {
            await sw.WriteAsync(JsonSerializer.Serialize(sounds, IndentedJsonSerializerOptions));
        }
    }

    private static async Task DownloadAssetsAsync(HttpClient httpClient, string assetsHost, AssetPlatform platform, string downloadPath,
        BundleManifest bundles, MovieManifest movies, SoundManifest sounds, int parallelDownloadsCount = 10)
    {
        AnsiConsole.WriteLine("Constructing URIs...");

        List<Uri> allFilesUris = ConstructUris(assetsHost, platform, bundles, movies, sounds);

        AnsiConsole.WriteLine("Downloading...");

        await AnsiConsole.Progress()
            .HideCompleted(true)
            .StartAsync(async context =>
            {
                SemaphoreSlim semaphoreSlim = new(parallelDownloadsCount);
                bool isPausedGlobally = false;

                ProgressTask globalProgressTask = context.AddTask("Global progress", true, allFilesUris.Count);

                await Task.WhenAll(allFilesUris.Select(DownloadAsset));

                async Task DownloadAsset(Uri uri)
                {
                    await semaphoreSlim.WaitAsync();

                    // ReSharper disable once LoopVariableIsNeverChangedInsideLoop
                    while (isPausedGlobally)
                        await Task.Delay(100);

                    ProgressTask progressTask = context.AddTask(uri.AbsolutePath);

                    try
                    {
                        using HttpResponseMessage response = await httpClient.GetAsync(uri);
                        await using Stream httpStream = await response.Content.ReadAsStreamAsync();

                        Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(downloadPath, uri.AbsolutePath[1..]))!);

                        await using StreamWriter fileWriter = new(Path.Combine(downloadPath, uri.AbsolutePath[1..]), false);

                        await httpStream.CopyToWithProgressAsync(fileWriter.BaseStream, response.Content.Headers.ContentLength,
                            progressTask);
                    }
                    catch (Exception ex)
                    {
                        isPausedGlobally = true;

                        AnsiConsole.WriteException(ex);

                        while (!AnsiConsole.Confirm("Continue?")) { }

                        isPausedGlobally = false;
                    }

                    progressTask.StopTask();
                    globalProgressTask.Increment(1);
                    semaphoreSlim.Release();
                }
            });
    }

    private static List<Uri> ConstructUris(string assetsHost, AssetPlatform platform, BundleManifest bundles, MovieManifest movies,
        SoundManifest sounds)
    {
        string platformString = AssetPlatformConverter.ToString(platform);

        List<Uri> result = [];

        foreach (BundleManifestEntry entry in bundles.Entries)
            result.Add(new Uri($"https://{assetsHost}/{platformString}/{entry.Hash}/{entry.Name}.unity3d"));

        foreach (MovieManifestEntry entry in movies.Entries)
        {
            if (entry.EnableSplit)
            {
                result.Add(new Uri($"https://{assetsHost}/{platformString}/{entry.Hash}/{entry.Name}.usm.ppart"));
                result.Add(new Uri($"https://{assetsHost}/{platformString}/{entry.Hash}/{entry.Name}.usm.spart"));
            }
            else
                result.Add(new Uri($"https://{assetsHost}/{platformString}/{entry.Hash}/{entry.Name}.usm"));
        }

        foreach (SoundManifestEntry entry in sounds.Entries)
        {
            if (entry.EnableSplit)
            {
                result.Add(new Uri($"https://{assetsHost}/{platformString}/{entry.Hash}/{entry.Name}.acb.ppart"));
                result.Add(new Uri($"https://{assetsHost}/{platformString}/{entry.Hash}/{entry.Name}.acb.spart"));
                result.Add(new Uri($"https://{assetsHost}/{platformString}/{entry.Hash}/{entry.Name}.awb.ppart"));
                result.Add(new Uri($"https://{assetsHost}/{platformString}/{entry.Hash}/{entry.Name}.awb.spart"));
            }
            else
            {
                result.Add(new Uri($"https://{assetsHost}/{platformString}/{entry.Hash}/{entry.Name}.acb"));
                if (entry.AwbHash is not "")
                    result.Add(new Uri($"https://{assetsHost}/{platformString}/{entry.Hash}/{entry.Name}.awb"));
            }
        }

        return result;
    }
}
