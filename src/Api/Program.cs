using Application.Abstractions;
using AutoMapper.Extensions.ExpressionMapping;
using Domain.Dtos;
using Domain.IRepositories;
using Domain.Mappings;
using Infrastructure.Application;
using Infrastructure.Domain.Repositories;
using Infrastructure.Domain.UnitOfWork;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.Configure<HolidaySettings>(builder.Configuration.GetSection(HolidaySettings.SectionName));

builder.Services.AddScoped<ICollaboratorRepository, CollaboratorRepository>();
builder.Services.AddScoped<IShiftRepository, ShiftRepository>();

builder.Services.AddAutoMapper(configuration =>
{
    configuration.AddExpressionMapping();
    configuration.AddProfile<CollaboratorMapping>();
    configuration.AddProfile<ShiftMapping>();
    AppDomain.CurrentDomain.GetAssemblies();
});

builder.Services.AddScoped<IHolidayDates, HolidayDates>();
builder.Services.AddScoped<ILoadShifts, LoadExcelShifts>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
