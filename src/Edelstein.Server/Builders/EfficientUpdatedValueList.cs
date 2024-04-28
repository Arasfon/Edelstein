using Edelstein.Data.Models.Components;

namespace Edelstein.Server.Builders;

public class EfficientUpdatedValueList
{
    public Gem? Gem { get; set; } = null;
    public Dictionary<uint, Card> CardList { get; set; } = [];
    public HashSet<CardSub> CardSubList { get; set; } = [];
    public Dictionary<uint, Item> ItemList { get; set; } = [];
    public Dictionary<PointType, Point> PointList { get; set; } = [];
    public HashSet<uint> MasterTitleIds { get; set; } = [];
    public HashSet<uint> MasterMusicIds { get; set; } = [];
    public HashSet<uint> MasterStampIds { get; set; } = [];
    public HashSet<EventPoint> EventPointList { get; set; } = [];
    public Stamina? Stamina { get; set; } = null;
    public HashSet<uint> MasterMovieIds { get; set; } = [];

    public UpdatedValueList ToUpdatedValueList() =>
        new()
        {
            Gem = Gem,
            CardList = CardList.Values.ToList(),
            CardSubList = CardSubList.ToList(),
            ItemList = ItemList.Values.ToList(),
            PointList = PointList.Values.ToList(),
            MasterTitleIds = MasterTitleIds.ToList(),
            MasterMusicIds = MasterMusicIds.ToList(),
            MasterStampIds = MasterStampIds.ToList(),
            EventPointList = EventPointList.ToList(),
            Stamina = Stamina,
            MasterMovieIds = MasterMovieIds.ToList()
        };
}
