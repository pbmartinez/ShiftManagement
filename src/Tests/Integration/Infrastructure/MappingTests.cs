using AutoMapper;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.Integration.WithDatabase
{
    public class MappingTests
    {
        [Test]
        public void AutoMapperConfiguration_IsValid()
        {
            var mapper = TestDatabase.Factory.Services.GetRequiredService<IMapper>();

            // Fails when an entity or DTO gets a member that no map handles.
            mapper.ConfigurationProvider.AssertConfigurationIsValid();
        }
    }
}
