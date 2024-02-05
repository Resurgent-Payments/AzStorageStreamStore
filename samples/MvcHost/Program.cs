using LvStreamStore;

var builder = WebApplication.CreateBuilder(args);

builder.Host.AddLvStreamStore()
    .UseEmbeddedClient()
    .UseMemoryStorage()
    .UseJsonSerialization()
    .AddApplicationToolkit()
        .RegisterSubscriber<BusinessDomain.ItemMsgHandlers>()
        .RegisterModel<MvcHost.Models.ItemsRm>()
        .UseWebHooks(opts => {
            opts.RegisterAssemblyFromType<MvcHost.Controllers.HomeController>();
        });

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

app.UseLvStreamStore();
app.UseApplicationToolkit();

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
