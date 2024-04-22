using Edelstein.Data.Models.Components;

namespace Edelstein.Server.Models;

public record AllChatsResult(
    List<ChatProgress> ChatProgresses,
    List<uint> ChatIds
);
