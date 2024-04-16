using Edelstein.Data.Constants;
using Edelstein.Data.Extensions;
using Edelstein.Data.Models;
using Edelstein.Data.Transport;
using Edelstein.GameServer.ActionResults;
using Edelstein.GameServer.Authorization;
using Edelstein.GameServer.Models;
using Edelstein.GameServer.Models.Endpoints.Chat;
using Edelstein.GameServer.Security;
using Edelstein.GameServer.Services;

using Microsoft.AspNetCore.Mvc;

namespace Edelstein.GameServer.Controllers;

[ApiController]
[Route("/api/chat")]
[ServiceFilter<RsaSignatureAuthorizationFilter>]
public class ChatController : Controller
{
    private readonly IChatService _chatService;

    public ChatController(IChatService chatService) =>
        _chatService = chatService;

    [HttpPost]
    [Route("home")]
    public async Task<EncryptedResult> Home()
    {
        ulong xuid = User.FindFirst(ClaimNames.Xuid).As<ulong>();

        AllChatsResult allChats = await _chatService.GetAll(xuid);

        return new EncryptedResponse<ChatHomeResponseData>(new ChatHomeResponseData(allChats.ChatProgresses, allChats.ChatIds,
            MasterStampIds.Get(), []));
    }

    [HttpPost]
    [Route("talk/start")]
    public async Task<EncryptedResult> TalkStart(EncryptedRequest<ChatTalkStartRequestData> encryptedRequest)
    {
        ulong xuid = User.FindFirst(ClaimNames.Xuid).As<ulong>();

        ChatProgressDocument chat = await _chatService.GetChat(xuid, encryptedRequest.DeserializedObject.ChatId,
            encryptedRequest.DeserializedObject.RoomId, encryptedRequest.DeserializedObject.ChapterId);

        return new EncryptedResponse<ChatTalkStartResponseData>(new ChatTalkStartResponseData(chat.SelectTalkIdList, chat.GetItemList,
            chat.ChatProgress.IsRead));
    }

    [HttpPost]
    [Route("talk/end")]
    public async Task<EncryptedResult> TalkEnd(EncryptedRequest<ChatTalkEndRequestData> encryptedRequest)
    {
        ulong xuid = User.FindFirst(ClaimNames.Xuid).As<ulong>();

        await _chatService.EndTalk(xuid, encryptedRequest.DeserializedObject.ChatId, encryptedRequest.DeserializedObject.RoomId,
            encryptedRequest.DeserializedObject.ChapterId, encryptedRequest.DeserializedObject.SelectTalkIdList);

        return EmptyEncryptedResponseFactory.Create();
    }
}
