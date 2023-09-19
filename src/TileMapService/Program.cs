using TileMapService;

var builder = WebApplication.CreateBuilder(args);

//
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = "ForbidScheme";
    options.DefaultForbidScheme = "ForbidScheme";
    options.AddScheme<SimpleAuthenticationHandler>("ForbidScheme", "Handle Forbidden");
});

builder.Services.AddCors();
builder.Services.AddControllers();
builder.Services.AddSingleton<ITileSourceFabric, TileSourceFabric>();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
// TODO: custom exception
////app.UseExceptionHandler(appError =>
////{
////    appError.Run(async context =>
////    {
////        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
////        context.Response.ContentType = "application/json";

////        var contextFeature = context.Features.Get<IExceptionHandlerFeature>();
////        if (contextFeature != null)
////        {
////            await context.Response.WriteAsync();
////        }
////    });
////});

app.UseCors(builder => builder.AllowAnyOrigin());
app.MapControllers();

var svc = app.Services.GetRequiredService<ITileSourceFabric>();
await svc.InitAsync();

app.Run();

namespace TileMapService
{
    public partial class Program { }
}
