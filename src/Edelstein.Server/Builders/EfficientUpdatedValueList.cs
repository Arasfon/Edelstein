using Edelstein.Data.Models.Components;

namespace Edelstein.Server.Builders;

public class EfficientUpdatedValueList
{
    public Gem? Gem { get; set; } = null;
    public Dictionary<uint, Card> CardList { get; set; } = [];
    public Dictionary<uint, CardSub> CardSubList { get; set; } = [];
    public Dictionary<uint, Item> ItemList { get; set; } = [];
    public Dictionary<PointType, Point> PointList { get; set; } = [];
    public HashSet<uint> MasterTitleIds { get; set; } = [];
    public HashSet<uint> MasterStampIds { get; set; } = [];
    public Dictionary<uint, EventPoint> EventPointList { get; set; } = [];
    public Stamina? Stamina { get; set; } = null;

    public UpdatedValueList ToUpdatedValueList() =>
        new()
        {
            Gem = Gem,
            CardList = CardList.Values.ToList(),
            CardSubList = CardSubList.Values.ToList(),
            ItemList = ItemList.Values.ToList(),
            PointList = PointList.Values.ToList(),
            MasterTitleIds = MasterTitleIds,
            MasterStampIds = MasterStampIds,
            EventPointList = EventPointList.Values.ToList(),
            Stamina = Stamina
        };
}
