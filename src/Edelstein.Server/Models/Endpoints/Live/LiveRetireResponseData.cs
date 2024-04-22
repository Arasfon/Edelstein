using Edelstein.Data.Models.Components;

namespace Edelstein.Server.Models.Endpoints.Live;

public record LiveRetireResponseData(
    Stamina Stamina,
    List<Item> ItemList,
    List<EventPoint> EventPointList
);
