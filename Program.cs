using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc;
using PIM.Helpers;
using PIM.Models;
using Microsoft.AspNetCore.Http.Features;
using ZennixApi.Data;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Cors;
using Microsoft.Extensions.FileProviders;
using System.IO;
using Microsoft.AspNetCore.StaticFiles; // ✅ ADICIONADO: Este é o using necessário

var builder = WebApplication.CreateBuilder(args);

// Configura Kestrel
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5115); // HTTP
    options.ListenLocalhost(7082, listenOptions => listenOptions.UseHttps()); // HTTPS
});

// ✅ ADICIONADO: CORS para subdomínio
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSubdomain", policy =>
    {
        policy.WithOrigins("https://api.suportezennix.com.br",
                          "https://suportezennix.com.br",
                          "http://localhost:5115",
                          "https://localhost:7082")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// ✅ Serviços para a API
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers(); // ✅ Para controllers API
builder.Services.AddLogging(); // ✅ Para ILogger nos controllers

// Serviços existentes
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<SessionCheckFilter>();
});

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 104857600; // 100 MB
});

builder.Services.AddHttpClient();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSession();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHostedService<ChamadoTriagemService>();

// ✅ CORRIGIDO: Removida duplicação do AddControllersWithViews
builder.Services.AddControllersWithViews()
    .AddJsonOptions(opts => opts.JsonSerializerOptions.PropertyNamingPolicy = null);

var app = builder.Build();

// Pipeline HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// ✅ ADICIONADO: Configuração do Static Files com MIME Type para APK
var contentTypeProvider = new FileExtensionContentTypeProvider();
contentTypeProvider.Mappings[".apk"] = "application/vnd.android.package-archive";
contentTypeProvider.Mappings[".msi"] = "application/x-msi";

app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = contentTypeProvider,
    OnPrepareResponse = ctx =>
    {
        // Permite cache para arquivos de download
        if (ctx.Context.Request.Path.StartsWithSegments("/downloads"))
        {
            ctx.Context.Response.Headers.Append("Cache-Control", "public, max-age=31536000");
        }
    }
});

// ✅ ADICIONADO: Use CORS - IMPORTANTE: deve vir antes de UseRouting
app.UseCors("AllowSubdomain");

app.UseRouting();
app.UseSession();
app.UseAuthorization();

// ✅ ADICIONADO: Mapeamento dos controllers API
app.MapControllers();

// Rota para MVC
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

public class SessionCheckFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        var path = context.HttpContext.Request.Path.ToString().ToLower();

        // ✅ ADICIONADO: Permite acesso sem login para rotas da API
        if (path.Contains("/login") || 
            path.Contains("/home/landing") || 
            path.StartsWith("/api/") ||
            path.StartsWith("/downloads/")) // ✅ Downloads não requerem sessão
            return;

        var usuario = context.HttpContext.Session.GetObjectFromJson<Usuario>("usuario");
        if (usuario == null)
        {
            context.Result = new RedirectToRouteResult(new RouteValueDictionary
            {
                { "controller", "Login" },
                { "action", "Index" }
            });
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}