using Edelstein.Data.Models.Components;

namespace Edelstein.GameServer.Models;

public record AllChatsResult(
    List<ChatProgress> ChatProgresses,
    List<uint> ChatIds
);
