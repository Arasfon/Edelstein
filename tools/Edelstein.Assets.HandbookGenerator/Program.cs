using Cocona;

using Edelstein.Assets.Management;
using Edelstein.Models.Manifest.Bundle;
using Edelstein.Models.Mst;

using Spectre.Console;

using System.Runtime.Serialization.Formatters.Binary;
using System.Text.Json;

#pragma warning disable SYSLIB0011

internal class Program
{
    public static async Task Main(string[] args) =>
        await CoconaLiteApp.RunAsync(HandleCoconaLiteApp);

    private static async Task HandleCoconaLiteApp(string jsonBundleManifestPath = "AndroidBundle.json",
        string extractedBundlePath = "mstBundle", string? decodeSingleExistingPath = null)
    {
        if (decodeSingleExistingPath is not null)
        {
            await HandleSingleMstDecode(decodeSingleExistingPath);
            return;
        }

        AnsiConsole.WriteLine("Loading bundle manifest...");

        BundleManifest bundleManifest = await LoadBundleManifestFromJson(jsonBundleManifestPath);

        AnsiConsole.WriteLine("Searching for Mst entry...");

        BundleManifestEntry? mstEntry = bundleManifest.Entries.FirstOrDefault(x => x.Identifier == "mst.ab");

        if (mstEntry is null)
        {
            AnsiConsole.MarkupLine("[red]Mst entry hasn't been found! Is manifest valid?[/]");
            return;
        }

        int mstPrefixLength = "Mst/".Length;
        List<string> assetNames = mstEntry.Assets.Select(x => x[mstPrefixLength..]).ToList();

        AnsiConsole.MarkupLine($"Mst bundle path is [green]{mstEntry.Hash}/{mstEntry.Name}.unity3d[/]");

        Directory.CreateDirectory(extractedBundlePath);

        AnsiConsole.MarkupLine($"Please extract bundle data (*.bytes files) to [green]{extractedBundlePath}[/] directory.");
        AnsiConsole.WriteLine("Continue after extraction.");

        while (!AnsiConsole.Confirm("Continue?")) { }

        List<string> missingAssets = FindMissingAssets(assetNames, extractedBundlePath).ToList();
        if (missingAssets.Count > 0)
        {
            AnsiConsole.MarkupLine("[red]Some assets are missing according to the manifest:[/]");
            foreach (string missingAsset in missingAssets)
                AnsiConsole.MarkupLine($"[red]{missingAsset}[/]");
            return;
        }

        BinaryFormatter binaryFormatter = new() { Binder = new MstSerializationBinder() };

        foreach (string assetPath in assetNames.Select(x => Path.Combine(extractedBundlePath, x)))
            await DeserialzeSingleMstBundle<dynamic>(assetPath);
    }

    private static async Task HandleSingleMstDecode(string decodeSingleExistingPath)
    {
        dynamic result = await DeserialzeSingleMstBundle<dynamic>(decodeSingleExistingPath);

        await using FileStream resultFileStream = new("SingleMstResult.json", FileMode.Create, FileAccess.Write);
        JsonSerializer.Serialize(resultFileStream, result);

        AnsiConsole.WriteLine("Success!");
    }

    private static async Task<BundleManifest> LoadBundleManifestFromJson(string filePath)
    {
        await using FileStream sr = new(filePath, FileMode.Open, FileAccess.Read);
        return (await JsonSerializer.DeserializeAsync<BundleManifest>(sr))!;
    }

    private static IEnumerable<string> FindMissingAssets(IEnumerable<string> expectedAssetNames, string extractedBundlePath)
    {
        IEnumerable<string> extractedDirectoryFiles = Directory.GetFiles(extractedBundlePath).Select(Path.GetFileName)!;

        return expectedAssetNames.Except(extractedDirectoryFiles);
    }

    private static async Task<T> DeserialzeSingleMstBundle<T>(string assetPath, BinaryFormatter? binaryFormatter = null)
    {
        binaryFormatter ??= new BinaryFormatter { Binder = new MstSerializationBinder() };

        await using FileStream assetFileStream = new(assetPath, FileMode.Open, FileAccess.Read);

        return (T)binaryFormatter.Deserialize(assetFileStream);
    }
}
