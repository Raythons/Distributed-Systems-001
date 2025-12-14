using cqrs_write_app.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Register RoundRobinService with replica connection strings
var replicaConnections = new[]
{
    "Host=postgres-replica-1;Port=5432;Database=cqrs_read;Username=admin;Password=password",
    "Host=postgres-replica-2;Port=5432;Database=cqrs_read;Username=admin;Password=password",
    "Host=postgres-replica-3;Port=5432;Database=cqrs_read;Username=admin;Password=password"
};
builder.Services.AddSingleton<RoundRobinService>(sp => new RoundRobinService(replicaConnections));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
