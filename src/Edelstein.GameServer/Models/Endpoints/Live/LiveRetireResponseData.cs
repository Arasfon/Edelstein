using Edelstein.Data.Models.Components;

namespace Edelstein.GameServer.Models.Endpoints.Live;

public record LiveRetireResponseData(
    Stamina Stamina,
    List<Item> ItemList,
    List<EventPoint> EventPointList
);
