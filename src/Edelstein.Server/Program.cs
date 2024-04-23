using Edelstein.Data.Configuration;
using Edelstein.Data.Msts.Persistence;
using Edelstein.Data.Serialization.Bson;
using Edelstein.Server.Authorization;
using Edelstein.Server.Configuration;
using Edelstein.Server.Configuration.OAuth;
using Edelstein.Server.ModelBinders;
using Edelstein.Server.Repositories;
using Edelstein.Server.Services;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Exceptions.Core;
using Serilog.Exceptions.Destructurers;
using Serilog.Exceptions.EntityFrameworkCore.Destructurers;
using Serilog.Exceptions.MongoDb.Destructurers;

using System.Net.Mime;

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
    builder.Services.Configure<SeqOptions>(builder.Configuration.GetSection("Seq"));
    builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection("Database"));
    builder.Services.Configure<MstDatabaseOptions>(builder.Configuration.GetSection("MstDatabase"));
    builder.Services.Configure<OAuthOptions>(builder.Configuration.GetSection("OAuth"));

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
    BsonSerializer.RegisterSerializer(new UInt64Serializer(BsonType.Int64));
    BsonSerializer.RegisterSerializer(new UInt32Serializer(BsonType.Int32));
    BsonSerializer.RegisterSerializer(new BooleanToIntegerBsonSerializer());

    ConventionPack camelCaseConvention = [new CamelCaseElementNameConvention()];
    ConventionRegistry.Register("CamelCase", camelCaseConvention, _ => true);

    builder.Services.AddSingleton<IMongoClient>(provider =>
    {
        DatabaseOptions databaseOptions = provider.GetRequiredService<IOptions<DatabaseOptions>>().Value;

        return new MongoClient(databaseOptions.ConnectionString);
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
    builder.Services.AddScoped<IUserMissionsRepository, UserMissionsRepository>();
    builder.Services.AddScoped<IUserInitializationDataRepository, UserInitializationDataRepository>();
    builder.Services.AddScoped<ILiveDataRepository, LiveDataRepository>();

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

    // Authorization filters
    builder.Services.AddScoped<RsaSignatureAuthorizationFilter>();
    builder.Services.AddScoped<OAuthHmacAuthorizationFilter>();
    builder.Services.AddScoped<OAuthRsaAuthorizationFilter>();

    // Controllers
    builder.Services.AddControllers(options =>
    {
        options.Filters.Add(new ResponseCacheAttribute
        {
            NoStore = true,
            Location = ResponseCacheLocation.None
        });

        options.ModelBinderProviders.Insert(0, new EncryptedRequestModelBinderProvider());
    });

    // Swagger
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // Configure the HTTP request pipeline
    WebApplication app = builder.Build();

    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();

    PhysicalFileProvider webRootFileProvider = new(app.Environment.WebRootPath);
    PhysicalFileProvider assetsFileProvider = new(app.Configuration["AssetsPath"] ?? app.Environment.WebRootPath);
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
        }
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
