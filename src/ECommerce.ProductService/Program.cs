using ECommerce.AI;
using ECommerce.Application.Interfaces;
using ECommerce.Application.Interfaces.Services;
using ECommerce.Application.Services;
using ECommerce.Infrastructure.Caching;
using ECommerce.Infrastructure.Persistence;
using ECommerce.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults("product-service");
builder.Services.AddProductPersistence(builder.Configuration);
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ICacheService, MemoryCacheService>();
builder.Services.AddFoundryAi(builder.Configuration);

builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();

var app = builder.Build();

await app.Services.InitializeProductDatabaseAsync(app.Configuration, app.Logger);

app.UseServiceDefaults();

app.Run();

public sealed class ProductServiceMarker;
