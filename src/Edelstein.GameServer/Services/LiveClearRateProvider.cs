using Edelstein.Data.Models.Components;
using Edelstein.GameServer.Models.Endpoints.Live;

using System.Text.Json;

namespace Edelstein.GameServer.Services;

public class LiveClearRateProvider : ILiveClearRateProvider
{
    private readonly IWebHostEnvironment _webHostEnvironment;

    public LiveClearRateProvider(IWebHostEnvironment webHostEnvironment) =>
        _webHostEnvironment = webHostEnvironment;

    public async Task<List<AllUserClearRate>> GetAll()
    {
        const string jsonFileName = "live_clear_rates.json";

        using StreamReader sr = new(Path.Combine(_webHostEnvironment.WebRootPath, jsonFileName));

        string moderationKeywordsJsonData = await sr.ReadToEndAsync();

        return JsonSerializer.Deserialize<LiveClearRateResponseData>(moderationKeywordsJsonData)!.AllUserClearRate;
    }
}
