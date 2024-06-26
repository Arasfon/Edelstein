using Edelstein.Data.Configuration;
using Edelstein.Data.Msts.Persistence;
using Edelstein.Data.Serialization.Bson;
using Edelstein.Data.Serialization.Json;
using Edelstein.Server.Authorization;
using Edelstein.Server.BackroundJobs;
using Edelstein.Server.Configuration;
using Edelstein.Server.Configuration.Assets;
using Edelstein.Server.Configuration.Metrics;
using Edelstein.Server.Configuration.OAuth;
using Edelstein.Server.ModelBinders;
using Edelstein.Server.Repositories;
using Edelstein.Server.Services;

using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using MongoDB.Driver.Core.Configuration;

using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

using Quartz;
using Quartz.AspNetCore;

using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Exceptions.Core;
using Serilog.Exceptions.Destructurers;
using Serilog.Exceptions.EntityFrameworkCore.Destructurers;
using Serilog.Exceptions.MongoDb.Destructurers;
using Serilog.Extensions.Logging;

using System.Net.Mime;
using System.Text.Json;

// Bootstrap logging
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/.log", rollingInterval: RollingInterval.Day)
    .Enrich.FromLogContext()
    .Enrich.WithExceptionDetails(new DestructuringOptionsBuilder()
        .WithDefaultDestructurers()
        .WithDestructurers(new IExceptionDestructurer[] { new DbUpdateExceptionDestructurer(), new MongoExceptionDestructurer() }))
    .CreateBootstrapLogger();

try
{
    // Configure services
    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

    // Configuration
    builder.Services.AddOptions<SeqOptions>().BindConfiguration("Seq");
    builder.Services.AddOptions<MetricsOptions>().BindConfiguration("Metrics");
    builder.Services.AddOptions<DatabaseOptions>().BindConfiguration("Database");
    builder.Services.AddOptions<MstDatabaseOptions>().BindConfiguration("MstDatabase");
    builder.Services.AddOptions<OAuthOptions>().BindConfiguration("OAuth");
    builder.Services.AddOptions<AssetsOptions>().BindConfiguration("Assets");

    // Logging
    builder.Services.AddSerilog((services, loggerConfiguration) =>
    {
        SeqOptions seqOptions = services.GetRequiredService<IOptions<SeqOptions>>().Value;

        if (builder.Environment.IsDevelopment())
            loggerConfiguration.MinimumLevel.Debug();
        else
            loggerConfiguration.MinimumLevel.Information();

        loggerConfiguration
            .WriteTo.Console(LogEventLevel.Information)
            .WriteTo.File("logs/.log", rollingInterval: RollingInterval.Day)
            .Enrich.FromLogContext()
            .Enrich.WithExceptionDetails(new DestructuringOptionsBuilder()
                .WithDefaultDestructurers()
                .WithDestructurers(new IExceptionDestructurer[] { new DbUpdateExceptionDestructurer(), new MongoExceptionDestructurer() }));

        if (seqOptions.Url != "")
            loggerConfiguration.WriteTo.Seq(seqOptions.Url, apiKey: seqOptions.ApiKey);
    });

    // Metrics
    builder.Logging.AddOpenTelemetry(options =>
    {
        options.IncludeScopes = true;
        options.IncludeFormattedMessage = true;
    });

    builder.Services.AddOpenTelemetry()
        .WithMetrics(meterProviderBuilder =>
        {
            meterProviderBuilder.AddPrometheusExporter(prometheusAspNetCoreOptions =>
                prometheusAspNetCoreOptions.DisableTotalNameSuffixForCounters = true);

            meterProviderBuilder.AddProcessInstrumentation();

            meterProviderBuilder.AddRuntimeInstrumentation()
                .AddMeter("Microsoft.AspNetCore.Hosting", "Microsoft.AspNetCore.Server.Kestrel");
        })
        .WithTracing(tracerProviderBuilder =>
        {
            if (builder.Environment.IsDevelopment())
                tracerProviderBuilder.SetSampler<AlwaysOnSampler>();

            tracerProviderBuilder.AddAspNetCoreInstrumentation();
            tracerProviderBuilder.AddHttpClientInstrumentation();
        });

    // Database
    BsonChunkPool.Default = new BsonChunkPool(256, 64 * 1024);

    BsonSerializer.RegisterSerializer(new UInt64Serializer(BsonType.Int64));
    BsonSerializer.RegisterSerializer(new UInt32Serializer(BsonType.Int32));
    BsonSerializer.RegisterSerializer(new BooleanToIntegerBsonSerializer());

    ConventionPack camelCaseConvention = [new CamelCaseElementNameConvention()];
    ConventionRegistry.Register("CamelCase", camelCaseConvention, _ => true);

    builder.Services.AddSingleton<IMongoClient>(provider =>
    {
        DatabaseOptions databaseOptions = provider.GetRequiredService<IOptions<DatabaseOptions>>().Value;

        MongoUrlBuilder urlBuilder = new(databaseOptions.ConnectionString);

        MongoClientSettings mongoClientSettings = new()
        {
            Scheme = urlBuilder.Scheme,
            Server = urlBuilder.Server,
            LoggingSettings = new LoggingSettings(new SerilogLoggerFactory())
        };

        return new MongoClient(mongoClientSettings);
    });

    // Mst database
    builder.Services.AddDbContext<MstDbContext>((provider, options) =>
    {
        MstDatabaseOptions databaseOptions = provider.GetRequiredService<IOptions<MstDatabaseOptions>>().Value;

        options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTrackingWithIdentityResolution);
        options.UseSqlite(databaseOptions.ConnectionString);
    });

    // Repositories
    builder.Services.AddScoped<ISequenceRepository<ulong>, UnsignedLongSequenceRepository>();
    builder.Services.AddScoped<IAuthenticationDataRepository, AuthenticationDataRepository>();
    builder.Services.AddScoped<IUserDataRepository, UserDataRepository>();
    builder.Services.AddScoped<IUserHomeRepository, UserHomeRepository>();
    builder.Services.AddScoped<IUserInitializationDataRepository, UserInitializationDataRepository>();
    builder.Services.AddScoped<ILiveDataRepository, LiveDataRepository>();
    builder.Services.AddScoped<IUserGiftsRepository, UserGiftsRepository>();

    // Services
    builder.Services.AddScoped<IUserService, UserService>();
    builder.Services.AddScoped<ILotteryService, LotteryService>();
    builder.Services.AddScoped<ITutorialService, TutorialService>();
    builder.Services.AddScoped<IDefaultGroupCardsFactoryService, DefaultGroupCardsFactoryService>();
    builder.Services.AddScoped<ILiveService, LiveService>();
    builder.Services.AddScoped<IChatService, ChatService>();

    builder.Services.AddHostedService<ConstantsLoaderService>();

    // Memory cache
    builder.Services.AddMemoryCache();

    // Background jobs
    builder.Services.AddScoped<ExpiredGiftsCleanupJob>();
    builder.Services.AddScoped<GiftHistoryCleanupJob>();

    // Quartz
    builder.Services.AddQuartz(q =>
    {
        JobKey expiredGiftsCleanupJobKey = new(nameof(ExpiredGiftsCleanupJob));
        q.AddJob<ExpiredGiftsCleanupJob>(options => options.WithIdentity(expiredGiftsCleanupJobKey));

        q.AddTrigger(options =>
                options
                    .ForJob(expiredGiftsCleanupJobKey)
                    .WithIdentity($"{nameof(ExpiredGiftsCleanupJob)}-trigger")
                    .StartNow()
            /*.WithCronSchedule("0 0 * * * ?")*/);

        JobKey giftHistoryCleanupJobKey = new(nameof(GiftHistoryCleanupJob));
        q.AddJob<GiftHistoryCleanupJob>(options => options.WithIdentity(giftHistoryCleanupJobKey));

        q.AddTrigger(options =>
                options
                    .ForJob(giftHistoryCleanupJobKey)
                    .WithIdentity($"{nameof(GiftHistoryCleanupJob)}-trigger")
                    .StartNow()
            /*.WithCronSchedule("0 0 * * * ?")*/);
    });

    builder.Services.AddQuartzServer(options =>
    {
        options.WaitForJobsToComplete = true;
    });

    // Forwarded headers
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders =
            ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    });

    // Json
    builder.Services.ConfigureHttpJsonOptions(options =>
    {
        options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    });

    // Authorization filters
    builder.Services.AddScoped<RsaSignatureAuthorizationFilter>();
    builder.Services.AddScoped<OAuthHmacAuthorizationFilter>();
    builder.Services.AddScoped<OAuthRsaAuthorizationFilter>();

    // Authorization middleware
    builder.Services.AddScoped<MetricsAuthorizationMiddleware>();

    // Response compression
    builder.Services.AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
        options.Providers.Add<BrotliCompressionProvider>();
        options.Providers.Add<GzipCompressionProvider>();
    });

    // Controllers
    builder.Services.AddControllers(options =>
        {
            options.Filters.Add(new ResponseCacheAttribute
            {
                NoStore = true,
                Location = ResponseCacheLocation.None
            });

            options.ModelBinderProviders.Insert(0, new EncryptedRequestModelBinderProvider());
        })
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;

            options.JsonSerializerOptions.Converters.Add(new BooleanToIntegerJsonConverter());
        });

    // Swagger
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // Configure the HTTP request pipeline
    WebApplication app = builder.Build();

    app.UseForwardedHeaders();

    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();

    app.UseResponseCompression();

    PhysicalFileProvider webRootFileProvider = new(app.Environment.WebRootPath);
    string assetsPath = app.Services.GetRequiredService<IOptions<AssetsOptions>>().Value.Path;
    PhysicalFileProvider assetsFileProvider = new(assetsPath == "" ? app.Environment.WebRootPath : assetsPath);
    CompositeFileProvider compositeFileProvider = new(webRootFileProvider, assetsFileProvider);

    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = compositeFileProvider,
        ContentTypeProvider = new FileExtensionContentTypeProvider
        {
            Mappings =
            {
                { ".unity3d", MediaTypeNames.Application.Octet },
                { ".ppart", MediaTypeNames.Application.Octet },
                { ".spart", MediaTypeNames.Application.Octet },
                { ".usm", MediaTypeNames.Application.Octet },
                { ".acb", MediaTypeNames.Application.Octet },
                { ".awb", MediaTypeNames.Application.Octet }
            }
        },
        OnPrepareResponse = context =>
        {
            string[] exts = [".unity3d", ".ppart", ".spart", ".usm", ".acb", ".awb"];

            if (exts.Contains(Path.GetExtension(context.File.Name)))
                context.Context.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
            else if (context.File.Name == "maintenance.json")
            {
                context.Context.Response.Headers.CacheControl = "no-store,no-cache";
                context.Context.Response.Headers.Pragma = "no-cache";
            }
        }
    });

    app.UseRouting();

    app.UseWhen(context => context.Request.Path.StartsWithSegments("/metrics"), builder =>
    {
        builder.UseMiddleware<MetricsAuthorizationMiddleware>();
    });

    app.MapPrometheusScrapingEndpoint();

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Process terminating");
}
finally
{
    await Log.CloseAndFlushAsync();
}
