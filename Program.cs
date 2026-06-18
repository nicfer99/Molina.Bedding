using Molina.Bedding.Mvc.DataAccess;
using Molina.Bedding.Mvc.Components;
using Molina.Bedding.Mvc.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromHours(8);
});

builder.Services.AddSingleton<IDbConnectionFactory, SqlDbConnectionFactory>();
builder.Services.AddScoped<IOperatorCatalogService, SqlOperatorCatalogService>();
builder.Services.AddScoped<IProductionLaunchService, SqlProductionLaunchService>();
builder.Services.AddScoped<IProductionDeclarationPersistenceService, SqlProductionDeclarationPersistenceService>();
builder.Services.AddScoped<IDeclarationNoteTypeCatalogService, SqlDeclarationNoteTypeCatalogService>();
builder.Services.AddScoped<IDeclarationDateAuthorizationService, DeclarationDateAuthorizationService>();
builder.Services.AddSingleton<IWorkMenuService, StaticWorkMenuService>();
builder.Services.AddScoped<BlazorProductionDeclarationState>();
builder.Services.AddScoped<IProductionDeclarationFlowService, ProductionDeclarationFlowService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapControllerRoute(
    name: "mvc-legacy",
    pattern: "mvc/{controller=ProductionDeclaration}/{action=Start}/{id?}");

app.MapControllerRoute(
    name: "mvc-explicit",
    pattern: "{controller}/{action=Start}/{id?}");

app.Run();
