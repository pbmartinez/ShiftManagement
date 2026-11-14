using Domain.IRepositories;
using Infrastructure.Domain.UnitOfWork;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;

namespace Tests.Integration.WithDatabase
{
    /// <summary>
    /// Runs once for every test in Tests.Integration.WithDatabase: starts a throwaway SQL Server container,
    /// applies the migrations, and removes the container at the end. Tests outside this namespace
    /// (e.g. Excel) don't need Docker.
    /// </summary>
    [SetUpFixture]
    public class TestDatabase
    {
        private const string DatabaseName = "ShiftManagement_Tests";
        private static MsSqlContainer _container = null!;

        public static ApiFactory Factory { get; private set; } = null!;

        [OneTimeSetUp]
        public async Task StartAsync()
        {
            _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();
            await _container.StartAsync();

            var connectionString = new SqlConnectionStringBuilder(_container.GetConnectionString())
            {
                InitialCatalog = DatabaseName
            }.ConnectionString;
            Factory = new ApiFactory(connectionString);

            using var scope = Factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            // Safety net: never migrate or wipe anything but the container database.
            db.Database.GetDbConnection().Database.Should().Be(DatabaseName);
            await db.Database.MigrateAsync();
        }

        [OneTimeTearDown]
        public async Task StopAsync()
        {
            await Factory.DisposeAsync();
            await _container.DisposeAsync();
        }

        public static async Task ResetAsync()
        {
            using var scope = Factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Shifts.ExecuteDeleteAsync();
            await db.Collaborators.ExecuteDeleteAsync();
        }

        public static async Task AddCollaboratorsAsync(params string[] emails)
        {
            using var scope = Factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<ICollaboratorRepository>();
            foreach (var email in emails)
                await repository.AddAsync(TestData.Collaborator(email));
            await repository.CommitChangesAsync();
        }

        /// <summary>Rows as in TestData.Shift; the collaborators must be added first.</summary>
        public static async Task<List<ShiftDto>> AddShiftsAsync(params string[] rows)
        {
            using var scope = Factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IShiftRepository>();
            var shifts = rows.Select(r => TestData.Shift(r)).ToList();
            foreach (var shift in shifts)
                await repository.AddAsync(shift);
            await repository.CommitChangesAsync();
            return shifts;
        }
    }
}
