using LvStreamStore;

var builder = WebApplication.CreateBuilder(args);

builder.Host.AddLvStreamStore()
    .UseEmbeddedClient()
    //.UseMemoryStorage()
    .UseLocalStorage(o => {
        o.BaseDataPath = "c:\\temp\\LvStreamStore";
        o.FileReadBlockSize = 1048576; // 1mb.
    })
    .UseJsonSerialization()
    .UseApplicationToolkit()
        .RegisterSubscriber<BusinessDomain.ItemMsgHandlers>()
        .RegisterModel<MvcHost.Models.ItemsRm>()
        .UseWebHooks(opts => {
            opts.RegisterAssemblyFromType<MvcHost.Controllers.HomeController>();
        });

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

app.UseLvStreamStore()
    .InitializeApplicationToolkit();

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
