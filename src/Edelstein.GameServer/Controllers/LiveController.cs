using Edelstein.Data.Constants;
using Edelstein.Data.Models;
using Edelstein.Data.Models.Components;
using Edelstein.GameServer.ActionResults;
using Edelstein.GameServer.Authorization;
using Edelstein.GameServer.Models.Endpoints.Live;
using Edelstein.GameServer.Security;
using Edelstein.GameServer.Services;

using Microsoft.AspNetCore.Mvc;

namespace Edelstein.GameServer.Controllers;

[ApiController]
[Route("/api/live")]
[ServiceFilter<RsaSignatureAuthorizationFilter>]
public class LiveController : Controller
{
    private readonly ILiveClearRateProvider _liveClearRateProvider;

    public LiveController(ILiveClearRateProvider liveClearRateProvider) =>
        _liveClearRateProvider = liveClearRateProvider;

    [Route("clearRate")]
    public async Task<EncryptedResult> ClearRate()
    {
        List<AllUserClearRate> clearRates = await _liveClearRateProvider.GetAll();

        return new EncryptedResponse<LiveClearRateResponseData>(
            new LiveClearRateResponseData(clearRates, MasterMusicIds.Get().ToList(), []));
    }

    [Route("guest")]
    public async Task<EncryptedResult> Guest(EncryptedRequest<LiveGuestRequestData> encryptedRequest)
    {
        List<AllUserClearRate> clearRates = await _liveClearRateProvider.GetAll();

        return new EncryptedResponse<LiveGuestResponseData>(new LiveGuestResponseData([Friend.GetTutorial()]));
    }
}
