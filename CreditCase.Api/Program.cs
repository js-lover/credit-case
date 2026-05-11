using System.Reflection;
using CreditCase.Api.Middleware;
using CreditCase.Application;
using CreditCase.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// localhost ve 127.0.0.1 aynı makinedir; tarayıcılar ikisini farklı origin olarak
// değerlendirdiğinden her ikisi de açıkça izin verilenler listesine alınır.
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.SetIsOriginAllowed(origin =>
    {
        var host = new Uri(origin).Host;
        return host == "localhost" || host == "127.0.0.1";
    })
    .AllowAnyHeader()
    .AllowAnyMethod()));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);
});

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseRouting();
app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
