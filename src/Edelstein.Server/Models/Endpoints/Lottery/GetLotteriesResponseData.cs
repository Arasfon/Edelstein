using Edelstein.Data.Models.Components;

namespace Edelstein.Server.Models.Endpoints.Lottery;

public record GetLotteriesResponseData(
    List<Data.Models.Components.Lottery> LotteryList,
    List<Item> ItemList
);
