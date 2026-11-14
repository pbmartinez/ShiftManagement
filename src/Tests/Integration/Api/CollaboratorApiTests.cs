using System.Net;
using System.Net.Http.Json;
using FluentAssertions.Execution;

namespace Tests.Integration.WithDatabase
{
    [NonParallelizable]
    public class CollaboratorApiTests
    {
        private HttpClient _client = null!;

        [SetUp]
        public async Task SetUp()
        {
            await TestDatabase.ResetAsync();
            _client = TestDatabase.Factory.CreateClient();
        }

        [TearDown]
        public void TearDown() => _client.Dispose();

        [Test]
        public async Task Post_ThenGet_ReturnsTheSameCollaborator()
        {
            var collaborator = TestData.Collaborator("ana@x.com");

            var post = await _client.PostAsJsonAsync("api/collaborator", collaborator);
            var get = await _client.GetAsync($"api/collaborator/{collaborator.Id}");

            using (new AssertionScope())
            {
                post.StatusCode.Should().Be(HttpStatusCode.Created);
                get.StatusCode.Should().Be(HttpStatusCode.OK);
                (await get.Content.ReadFromJsonAsync<CollaboratorDto>()).Should().BeEquivalentTo(collaborator);
            }
        }

        [TestCase("ana.new@x.com", HttpStatusCode.NoContent)]
        [TestCase("not-an-email", HttpStatusCode.BadRequest)]
        public async Task Put_ValidatesTheEmail(string newEmail, HttpStatusCode expectedStatus)
        {
            await TestDatabase.AddCollaboratorsAsync("ana@x.com");
            var changed = TestData.Collaborator("ana@x.com");
            changed.Email = newEmail;

            var response = await _client.PutAsJsonAsync($"api/collaborator/{changed.Id}", changed);

            response.StatusCode.Should().Be(expectedStatus);
        }
    }
}
