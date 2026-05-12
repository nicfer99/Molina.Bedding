using Molina.Bedding.Mvc.DataAccess;
using Molina.Bedding.Mvc.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
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
builder.Services.AddScoped<IDeclarationDateAuthorizationService, DeclarationDateAuthorizationService>();
builder.Services.AddSingleton<IWorkMenuService, StaticWorkMenuService>();

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

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=ProductionDeclaration}/{action=Start}/{id?}");

app.Run();
