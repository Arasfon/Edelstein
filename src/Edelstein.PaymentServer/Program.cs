using Edelstein.Protocol;

using Microsoft.AspNetCore.Http.Extensions;

using System.Text.Json;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
});

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

const long AppId = 232610769078541;
const long XAppId = 100900301;
Dictionary<string, string> tokens = new();
Dictionary<string, string> uidToXuid = new();

app.MapPost("/v1.0/auth/initialize", async (HttpContext context) =>
{
    using StreamReader sr = new(context.Request.Body);
    string body = await sr.ReadToEndAsync();
    if (!VerifyHmacOAuth(context.Request.Method, context.Request.GetDisplayUrl(), body, context.Request.Headers["Authorization"]!))
        throw new ArgumentException();

    AuthInitializeRequestData data = JsonSerializer.Deserialize<AuthInitializeRequestData>(body,
        new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower })!;

    const string userId = "b1b5dc401a45101845459d2c7f5ed2da"; // 32 symbol random hex string?

    // data.Token is in PEM format
    string publicKey = data.Token
        .Replace("-----BEGIN PUBLIC KEY-----", "")
        .Replace("-----END PUBLIC KEY-----", "")
        .Replace("\r", "")
        .Replace("\n", "");

    // Register user
    tokens[userId] = publicKey;
    uidToXuid[userId] = "251330662043447";

    // TODO: Move to response filter or something
    context.Response.Headers["X-GREE-Authorization"] = BuildResponseOAuth(context.Request.Method, context.Request.GetDisplayUrl(),
        context.Request.Headers["Authorization"]!);

    return Results.Text($$"""{"result":"OK","app_id":"{{AppId}}","uuid":"{{AppId}}{{userId}}"}""", "application/json");
});

app.MapGet("/v1.0/auth/x_uid", async (HttpContext context) =>
{
    using StreamReader sr = new(context.Request.Body);
    string body = await sr.ReadToEndAsync();
    Dictionary<string, string> requestOauth = OAuth.BuildOAuthValuesFromHeader(context.Request.Headers["Authorization"]!);
    if (!OAuth.TryGetUserIdFromXOAuth(requestOauth["xoauth_requestor_id"], out string? userId))
        throw new ArgumentException();
    string userPublicKey = tokens[userId];
    if (!VerifyRsaOAuth(context.Request.Method, context.Request.GetDisplayUrl(), body, requestOauth, userPublicKey))
        throw new ArgumentException();

    string xuid = uidToXuid[userId];

    context.Response.Headers["X-GREE-Authorization"] = BuildResponseOAuth(context.Request.Method, context.Request.GetDisplayUrl(),
        context.Request.Headers["Authorization"]!);

    return Results.Text($$"""{"result":"OK","x_uid":"{{xuid}}","x_app_id":"{{XAppId}}"}""", "application/json");
});

app.MapPost("/v1.0/auth/authorize", async (HttpContext context) =>
{
    using StreamReader sr = new(context.Request.Body);
    string body = await sr.ReadToEndAsync();
    Dictionary<string, string> requestOauth = OAuth.BuildOAuthValuesFromHeader(context.Request.Headers["Authorization"]!);
    if (!OAuth.TryGetUserIdFromXOAuth(requestOauth["xoauth_requestor_id"], out string? userId))
        throw new ArgumentException();
    string userPublicKey = tokens[userId];
    if (!VerifyRsaOAuth(context.Request.Method, context.Request.GetDisplayUrl(), body, requestOauth, userPublicKey))
        throw new ArgumentException();

    context.Response.Headers["X-GREE-Authorization"] = BuildResponseOAuth(context.Request.Method, context.Request.GetDisplayUrl(),
        context.Request.Headers["Authorization"]!);

    return Results.Text("""{"result":"OK"}""", "application/json");
});

app.Run();
return;

static bool VerifyHmacOAuth(string httpMethod, string url, string body, string requestAuthorizationHeader)
{
#if DEBUG
    url = url.Replace("localhost:7120", "sif2-payment.example.com");
#endif

    Dictionary<string, string> requestOauth = OAuth.BuildOAuthValuesFromHeader(requestAuthorizationHeader);

    return OAuth.VerifyOAuthHmac(httpMethod, url, body, requestOauth);
}

static bool VerifyRsaOAuth(string httpMethod, string url, string body, Dictionary<string, string> requestOauth, string publicKey)
{
#if DEBUG
    url = url.Replace("localhost:7120", "sif2-payment.example.com");
#endif

    return OAuth.VerifyOAuthRsa(httpMethod, url, body, requestOauth, publicKey);
}

static string BuildResponseOAuth(string httpMethod, string url, string requestAuthorizationHeader)
{
    url = url.Replace("localhost:7120", "sif2-payment.example.com");

    Dictionary<string, string> requestOauth = OAuth.BuildOAuthValuesFromHeader(requestAuthorizationHeader);

    Dictionary<string, string> oauthValues = new()
    {
        ["oauth_version"] = "1.0",
        ["oauth_nonce"] = "49613a9cec131cccdd507a429758c20c", // TODO: generate randomly
        ["oauth_timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
        ["oauth_consumer_key"] = AppId.ToString(),
        ["oauth_signature_method"] = "HMAC-SHA1",
        ["oauth_body_hash"] = requestOauth["oauth_body_hash"]
    };

    OAuth.AppendSignature(httpMethod, url, oauthValues);

    return OAuth.BuildOAuthHeader(oauthValues);
}

public record AuthInitializeRequestData(
    string DeviceId,
    string Token,
    string Payload
);
