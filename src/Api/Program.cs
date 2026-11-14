using Application.Abstractions;
using Application.Services;
using Application.Validators;
using AutoMapper.Extensions.ExpressionMapping;
using Domain.Dtos;
using Domain.IRepositories;
using Domain.Mappings;
using Infrastructure.Application;
using Infrastructure.Domain.Repositories;
using Infrastructure.Domain.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddOpenApi();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.Configure<HolidaySettings>(builder.Configuration.GetSection(HolidaySettings.SectionName));

builder.Services.AddScoped<ICollaboratorRepository, CollaboratorRepository>();
builder.Services.AddScoped<IShiftRepository, ShiftRepository>();
builder.Services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
builder.Services.AddScoped<IShiftImportService, ShiftImportService>();

builder.Services.AddScoped<IRepository<ShiftDto>>(sp => sp.GetRequiredService<IShiftRepository>());
builder.Services.AddScoped<IRepository<CollaboratorDto>>(sp => sp.GetRequiredService<ICollaboratorRepository>());
builder.Services.AddScoped(typeof(IEntityWriter<>), typeof(EntityWriter<>));

builder.Services.AddScoped<IBusinessValidator<ShiftDto>, ShiftOverlapValidator>();

builder.Services.AddAutoMapper(configuration =>
{
    configuration.AddExpressionMapping();
    configuration.AddProfile<CollaboratorMapping>();
    configuration.AddProfile<ShiftMapping>();
    AppDomain.CurrentDomain.GetAssemblies();
});

// Nager.Date client: answers cached in memory, failed calls retried by Polly (settings in HolidaySettings:Retry).
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient<IHolidayDates, HolidayDates>((sp, client) =>
        client.BaseAddress = new Uri(sp.GetRequiredService<IOptions<HolidaySettings>>().Value.NagerEndpoint))
    .AddResilienceHandler("nager", pipeline => pipeline.AddRetry(
        builder.Configuration.GetSection("HolidaySettings:Retry").Get<HttpRetryStrategyOptions>()!));
builder.Services.AddScoped<ILoadShifts, LoadExcelShifts>();
builder.Services.AddScoped<IExportShifts, ExportExcelShifts>();

var app = builder.Build();

// Serves the built Angular app (wwwroot) in production, so UI and API share one origin.
app.UseDefaultFiles();
app.MapStaticAssets();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("/index.html");

app.Run();

// Lets the integration tests start the API with WebApplicationFactory<Program>.
public partial class Program;
