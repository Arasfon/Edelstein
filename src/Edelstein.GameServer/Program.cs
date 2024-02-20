WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Configure services
if (builder.Environment.IsDevelopment())
    builder.Services.AddHttpLogging(o => { });

builder.Services.AddControllers();

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

app.Run();
