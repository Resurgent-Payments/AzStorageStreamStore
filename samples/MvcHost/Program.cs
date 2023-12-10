using LvStreamStore;
using LvStreamStore.ApplicationToolkit;

var builder = WebApplication.CreateBuilder(args);

builder.Host.AddLvStreamStore()
    .UseEmbeddedClient()
    .UseMemoryStorage()
    .UseJsonSerialization()
    .UseApplicationToolkit()
    .UseWebHooks(opts => {
        opts.RegisterAssemblyFromType<MvcHost.Controllers.HomeController>();
    });

// Add services to the container.
builder.Services.AddControllersWithViews();

// add command/event services.
builder.Host.ConfigureServices((ctx, services) => {
    services.AddSingleton<BusinessDomain.ItemMsgHandlers>();
    services.AddSingleton<IAutoStartService>(sp => sp.GetRequiredService<BusinessDomain.ItemMsgHandlers>());

    services.AddSingleton<MvcHost.Models.ItemsRm>();
    services.AddSingleton<IAutoStartService>(sp => sp.GetRequiredService<MvcHost.Models.ItemsRm>());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment()) {
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
