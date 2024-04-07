using Edelstein.Data.Configuration;
using Edelstein.Data.Serialization.Bson;
using Edelstein.GameServer.Authorization;
using Edelstein.GameServer.ModelBinders;
using Edelstein.GameServer.Repositories;
using Edelstein.GameServer.Services;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

using System.Net.Mime;

// Configure services
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Configuration
builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection("Database"));

// Logging
if (builder.Environment.IsDevelopment())
    builder.Services.AddHttpLogging(_ => { });

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

// Repositories
builder.Services.AddScoped<IAuthenticationDataRepository, AuthenticationDataRepository>();
builder.Services.AddScoped<IUserDataRepository, UserDataRepository>();

// Services
builder.Services.AddScoped<IUserService, UserService>();

// Authorization filters
builder.Services.AddScoped<RsaSignatureAuthorizationFilter>();

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

if (app.Environment.IsDevelopment())
{
    app.UseHttpLogging();

    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

PhysicalFileProvider webRootFileProvider = new(app.Environment.WebRootPath);
PhysicalFileProvider assetsFileProvider = new(app.Configuration["AssetsPath"] ?? app.Environment.WebRootPath);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new CompositeFileProvider(webRootFileProvider, assetsFileProvider),
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

app.MapControllers();

app.Run();
