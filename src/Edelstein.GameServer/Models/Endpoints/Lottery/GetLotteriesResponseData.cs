using Edelstein.Data.Models.Components;

namespace Edelstein.GameServer.Models.Endpoints.Lottery;

public record GetLotteriesResponseData(
    List<Data.Models.Components.Lottery> LotteryList,
    List<Item> ItemList
);
