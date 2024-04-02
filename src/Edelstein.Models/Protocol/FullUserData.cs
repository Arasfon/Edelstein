using Edelstein.Models.Serialization;

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

using OneOf;

using System.Text.Json.Serialization;

namespace Edelstein.Models.Protocol;

public class FullUserData
{
    [JsonIgnore]
    public ObjectId Id { get; set; }

    public required User User { get; set; }
    public required Gem Gem { get; set; }
    public List<Card> CardList { get; set; } = [];
    public List<CardSub> CardSubList { get; set; } = [];
    public List<Deck> DeckList { get; set; } = [];
    public List<Item> ItemList { get; set; } = [];
    public List<Point> PointList { get; set; } = [];
    public required Stamina Stamina { get; set; }
    public List<Character> CharacterList { get; set; } = [];
    public List<Story> StoryList { get; set; } = [];
    public List<Live> LiveList { get; set; } = [];
    public List<LiveMission> LiveMissionList { get; set; } = [];
    public List<uint> MasterMusicIds { get; set; } = [];
    public List<Group> GroupList { get; set; } = [];
    public List<uint> MasterStampIds { get; set; } = [];
    public uint TutorialStep { get; set; }
    public List<EventPoint> EventPointList { get; set; } = [];
    public long StartTime { get; set; }
    public List<uint> MasterTitleIds { get; set; } = [];
    public List<Lottery> LotteryList { get; set; } = [];
    public List<Shop> ShopList { get; set; } = [];
    public List<FunctionLock> FunctionLockList { get; set; } = [];

    [BsonSerializer(typeof(OneOfListOrObjectBsonSerializer<Movie>))]
    public OneOf<List<Movie>, Movie> Movie { get; set; } = new List<Movie>();

    public List<Subscription> SubscriptionList { get; set; } = [];

    [BsonSerializer(typeof(OneOfListOrObjectBsonSerializer<Membership>))]
    public OneOf<List<Membership>, Membership> Membership { get; set; } = new List<Membership>();
}
