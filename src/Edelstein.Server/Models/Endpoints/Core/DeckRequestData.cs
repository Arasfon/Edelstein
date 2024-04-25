namespace Edelstein.Server.Models.Endpoints.Core;

public record DeckRequestData(byte Slot, List<ulong> MainCardIds);
