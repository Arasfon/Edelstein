using Edelstein.Data.Configuration;
using Edelstein.Data.Serialization.Bson;
using Edelstein.Data.Serialization.Json;
using Edelstein.PaymentServer.Authorization;
using Edelstein.PaymentServer.Configuration.OAuth;
using Edelstein.PaymentServer.Repositories;
using Edelstein.PaymentServer.Services;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

using System.Text.Json;

// Configure services
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Configuration
builder.Services.Configure<OAuthOptions>(builder.Configuration.GetSection("OAuth"));
builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection("Database"));

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
});

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
builder.Services.AddScoped<ISequenceRepository<ulong>, UnsignedLongSequenceRepository>();
builder.Services.AddScoped<IAuthenticationDataRepository, AuthenticationDataRepository>();
builder.Services.AddScoped<IUserDataRepository, UserDataRepository>();

// Services
builder.Services.AddScoped<IUserService, UserService>();

// Memory cache
builder.Services.AddMemoryCache();

// Authorization filters
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

if (app.Environment.IsDevelopment())
{
    app.UseHttpLogging();

    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
