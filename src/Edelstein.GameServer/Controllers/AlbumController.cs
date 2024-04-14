using Edelstein.GameServer.ActionResults;
using Edelstein.GameServer.Authorization;
using Edelstein.GameServer.Security;

using Microsoft.AspNetCore.Mvc;

using System.Text.Json;

namespace Edelstein.GameServer.Controllers;

[ApiController]
[Route("/api/album")]
[ServiceFilter<RsaSignatureAuthorizationFilter>]
public class AlbumController : Controller
{
    private readonly IWebHostEnvironment _webHostEnvironment;

    public AlbumController(IWebHostEnvironment webHostEnvironment) =>
        _webHostEnvironment = webHostEnvironment;

    [Route("sif")]
    public async Task<EncryptedResult> Sif(EncryptedRequest encryptedRequest) =>
        new EncryptedResponse<dynamic>(new
        {
            cards = JsonSerializer.Deserialize<List<dynamic>>(
                await System.IO.File.ReadAllTextAsync(Path.Join(_webHostEnvironment.WebRootPath, "album_sif.json")))!
        });
}
