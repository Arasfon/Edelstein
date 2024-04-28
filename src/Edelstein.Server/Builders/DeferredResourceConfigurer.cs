using Edelstein.Data.Models;
using Edelstein.Data.Models.Components;

namespace Edelstein.Server.Builders;

public class DeferredResourceConfigurer : ResourceConfigurer
{
    private bool _shouldBeAdded = true;

    public Reward ConfiguredReward => Reward!;

    public DeferredResourceConfigurer(IResourceAdditionBuilder resourceAdditionBuilder, bool isResourceNew, Reward reward,
        ExchangeItem? exchangeItem = null) : base(resourceAdditionBuilder, isResourceNew, reward, exchangeItem) { }

    public override DeferredResourceConfigurer SetDropInfo(DropInfo dropInfo)
    {
        base.SetDropInfo(dropInfo);
        return this;
    }

    public virtual DeferredResourceConfigurer MakeLimited(List<LimitedReward> limitedRewards, int maxAmount, int firstReward = 0)
    {
        if (!_shouldBeAdded)
            return this;

        if (Reward is null)
            return this;

        // TODO: Use actual live clear reward ids
        // TODO: Use Dictionary<uint, int> instead of List<LimitedReward>
        LimitedReward? limitedReward = limitedRewards.FirstOrDefault(x => x.MasterRewardId == Reward.Value);

        if (limitedReward is null)
        {
            SetDropInfo(new DropInfo
            {
                FirstReward = firstReward,
                GetableCount = Reward.Amount,
                RemainingGetableCount = maxAmount - Reward.Amount
            });

            limitedRewards.Add(new LimitedReward
            {
                MasterRewardId = Reward.Value,
                Remaining = maxAmount - Reward.Amount
            });
        }
        else
        {
            if (limitedReward.Remaining == 0)
            {
                _shouldBeAdded = false;
                return this;
            }

            if (limitedReward.Remaining - Reward.Amount < 0)
                Reward.Amount = limitedReward.Remaining;

            limitedReward.Remaining -= Reward.Amount;

            SetDropInfo(new DropInfo
            {
                FirstReward = firstReward,
                GetableCount = maxAmount - limitedReward.Remaining,
                RemainingGetableCount = limitedReward.Remaining
            });
        }

        return this;
    }

    public virtual ResourceConfigurer Finish()
    {
        if (_shouldBeAdded)
            return ResourceAdditionBuilder.FinishDeferred(this);

        IsResourceNew = false;
        return this;
    }
}
