using Edelstein.Server.Authorization;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

using System.Text.Json.Nodes;

namespace Edelstein.Server.Controllers;

[ApiController]
[Route("/v1.0/moderate")]
[ServiceFilter<OAuthRsaAuthorizationFilter>]
public class ModerationController : Controller
{
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly IMemoryCache _memoryCache;

    public ModerationController(IWebHostEnvironment webHostEnvironment, IMemoryCache memoryCache)
    {
        _webHostEnvironment = webHostEnvironment;
        _memoryCache = memoryCache;
    }

    [Route("keywordlist")]
    public async Task<IActionResult> KeywordList()
    {
        const string cachingKey = "ModerationKeywords";
        const string jsonFileName = "moderation_keywords.json";

        if (!_memoryCache.TryGetValue(cachingKey, out JsonNode? moderationKeywords))
        {
            using StreamReader sr = new(Path.Combine(_webHostEnvironment.WebRootPath, jsonFileName));

            string moderationKeywordsJsonData = await sr.ReadToEndAsync();

            moderationKeywords = JsonNode.Parse(moderationKeywordsJsonData)!;

            _memoryCache.Set(cachingKey, moderationKeywords, new MemoryCacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(10) });
        }

        return Ok(new
        {
            result = "OK",
            entry = new
            {
                timestamp = $"{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}",
                keywords = moderationKeywords!["keywords"]
            }
        });
    }

    [HttpPost]
    [Route("filtering/commit")]
    public IActionResult FilteringCommit() =>
        Ok(new { result = "OK" });
}
