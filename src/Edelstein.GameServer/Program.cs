using Edelstein.GameServer.ModelBinders;

using Microsoft.Extensions.FileProviders;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Configure services
if (builder.Environment.IsDevelopment())
    builder.Services.AddHttpLogging(o => { });

builder.Services.AddControllers(options =>
{
    options.ModelBinderProviders.Insert(0, new EncryptedRequestModelBinderProvider());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseHttpLogging();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

PhysicalFileProvider webRootProvider =
    new PhysicalFileProvider(builder.Environment.WebRootPath);

PhysicalFileProvider assetsFileProvider =
    new PhysicalFileProvider(app.Configuration["AssetsPath"] ?? builder.Environment.WebRootPath);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new CompositeFileProvider(webRootProvider, assetsFileProvider),
    ServeUnknownFileTypes = true
});

app.Run();
