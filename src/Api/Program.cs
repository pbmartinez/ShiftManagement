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

builder.Services.AddScoped<IHolidayDates, HolidayDates>();
builder.Services.AddScoped<ILoadShifts, LoadExcelShifts>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
