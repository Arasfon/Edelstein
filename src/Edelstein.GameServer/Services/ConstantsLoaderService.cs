using Edelstein.Data.Constants;
using Edelstein.Data.Msts.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Edelstein.GameServer.Services;

public class ConstantsLoaderService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;

    public ConstantsLoaderService(IServiceProvider serviceProvider) =>
        _serviceProvider = serviceProvider;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = _serviceProvider.CreateScope();

        MstDbContext mstDbContext = scope.ServiceProvider.GetRequiredService<MstDbContext>();

        List<uint> masterMusicIds = await mstDbContext.MusicMsts.Select(x => x.Id).Distinct().ToListAsync(cancellationToken);
        MasterMusicIds.Set(masterMusicIds);

        List<uint> masterStampIds = await mstDbContext.StampMsts.Select(x => x.Id).Distinct().ToListAsync(cancellationToken);
        MasterStampIds.Set(masterStampIds);
    }

    public Task StopAsync(CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
