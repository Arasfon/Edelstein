using Edelstein.GameServer.ModelBinders;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;

using System.Net.Mime;

// Configure services
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Logging
if (builder.Environment.IsDevelopment())
    builder.Services.AddHttpLogging(_ => { });

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
            { "unity3d", MediaTypeNames.Application.Octet },
            { "ppart", MediaTypeNames.Application.Octet },
            { "spart", MediaTypeNames.Application.Octet },
            { "usm", MediaTypeNames.Application.Octet },
            { "acb", MediaTypeNames.Application.Octet },
            { "awb", MediaTypeNames.Application.Octet }
        }
    }
});

app.MapControllers();

app.Run();
