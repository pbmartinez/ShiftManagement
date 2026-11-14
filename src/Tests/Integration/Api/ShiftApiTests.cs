using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions.Execution;

namespace Tests.Integration.WithDatabase
{
    [NonParallelizable]
    public class ShiftApiTests
    {
        private HttpClient _client = null!;
        private ShiftDto _anaShift = null!;

        [SetUp]
        public async Task SetUp()
        {
            await TestDatabase.ResetAsync();
            await TestDatabase.AddCollaboratorsAsync("ana@x.com", "bob@x.com");
            var shifts = await TestDatabase.AddShiftsAsync("ana@x.com|2026-11-13|09:00|10:00", "bob@x.com|2026-11-13|12:00|13:00");
            _anaShift = shifts[0];
            _client = TestDatabase.Factory.CreateClient();
        }

        [TearDown]
        public void TearDown() => _client.Dispose();

        // A request body as the frontend sends it: the collaborator by id only.
        private static ShiftDto Body(string row)
        {
            var shift = TestData.Shift(row);
            shift.Collaborator = null;
            return shift;
        }

        private static async Task AssertResponse(HttpResponseMessage response, HttpStatusCode expectedStatus, string? expectedText)
        {
            var body = await response.Content.ReadAsStringAsync();
            using (new AssertionScope())
            {
                response.StatusCode.Should().Be(expectedStatus, body);
                if (expectedText is not null)
                    body.Should().Contain(expectedText);
            }
        }

        [TestCase("10:00", "11:00", HttpStatusCode.Created, null, TestName = "POST a free slot")]
        [TestCase("09:30", "10:30", HttpStatusCode.BadRequest, "overlaps existing shift ana@x.com 2026-11-13 09:00-10:00", TestName = "POST an overlapping shift")]
        [TestCase("11:00", "10:00", HttpStatusCode.BadRequest, "EndTime must be after StartTime", TestName = "POST end before start")]
        public async Task Post(string start, string end, HttpStatusCode expectedStatus, string? expectedText)
        {
            var response = await _client.PostAsJsonAsync("api/Shift", Body($"ana@x.com|2026-11-13|{start}|{end}"));

            await AssertResponse(response, expectedStatus, expectedText);
        }

        [TestCase("ana@x.com", "09:00", "11:00", HttpStatusCode.NoContent, null, TestName = "PUT a new time on your own shift")]
        [TestCase("bob@x.com", "09:00", "10:00", HttpStatusCode.NoContent, null, TestName = "PUT the shift on a free collaborator")]
        [TestCase("bob@x.com", "12:30", "13:30", HttpStatusCode.BadRequest, "overlaps existing shift bob@x.com 2026-11-13 12:00-13:00", TestName = "PUT the shift on a busy collaborator")]
        public async Task Put(string email, string start, string end, HttpStatusCode expectedStatus, string? expectedText)
        {
            var response = await _client.PutAsJsonAsync($"api/Shift/{_anaShift.Id}", Body($"{email}|2026-11-13|{start}|{end}"));

            await AssertResponse(response, expectedStatus, expectedText);
        }

        [TestCase(HttpStatusCode.OK, "\"imported\":2", "ana@x.com|2026-11-14|09:00|10:00", "ANA@x.com|2026-11-14|10:00|11:00", TestName = "import a clean file")]
        [TestCase(HttpStatusCode.BadRequest, "in the same request", "ana@x.com|2026-11-14|09:00|10:00", "ana@x.com|2026-11-14|09:30|10:30", TestName = "import an overlap inside the file")]
        [TestCase(HttpStatusCode.BadRequest, "overlaps existing shift", "ana@x.com|2026-11-13|09:30|10:30", TestName = "import an overlap with the database")]
        [TestCase(HttpStatusCode.BadRequest, "No collaborator found with email carl@x.com", "carl@x.com|2026-11-14|09:00|10:00", TestName = "import an unknown email")]
        [TestCase(HttpStatusCode.BadRequest, "The file contains no shifts", TestName = "import a file with only the header")]
        public async Task Import(HttpStatusCode expectedStatus, string expectedText, params string[] rows)
        {
            var file = new ByteArrayContent(TestData.ExcelBytes("ExcelTime", rows));
            file.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            using var form = new MultipartFormDataContent { { file, "file", "shifts.xlsx" } };

            var response = await _client.PostAsync("api/Shift/import", form);

            await AssertResponse(response, expectedStatus, expectedText);
            var collaborators = await _client.GetFromJsonAsync<List<CollaboratorDto>>("api/collaborator");
            collaborators.Should().HaveCount(2, "an import must never create collaborators");
        }

        // ---- Known gaps: the behaviour we want, skipped until fixed ----

        [Test, Ignore("Known gap: JsonPatchDocument needs AddNewtonsoftJson() to bind the request body")]
        public async Task Patch_ChangesOneField()
        {
            var patch = new StringContent("""[{ "op": "replace", "path": "/endTime", "value": "11:00:00" }]""",
                Encoding.UTF8, "application/json-patch+json");

            var response = await _client.PatchAsync($"api/Shift/{_anaShift.Id}", patch);

            await AssertResponse(response, HttpStatusCode.NoContent, null);
        }

        [Test, Ignore("Known gap: Entity.Id is 'required', so a POST without an id is rejected")]
        public async Task Post_WithoutId_IsCreated()
        {
            var response = await _client.PostAsJsonAsync("api/Shift", new
            {
                collaboratorId = TestData.IdOf("ana@x.com"),
                date = "2026-11-14",
                startTime = "09:00:00",
                endTime = "10:00:00"
            });

            await AssertResponse(response, HttpStatusCode.Created, null);
        }

        [Test, Ignore("Known gap: an unknown CollaboratorId fails on the foreign key (500) instead of a 400")]
        public async Task Post_UnknownCollaborator_IsBadRequest()
        {
            var response = await _client.PostAsJsonAsync("api/Shift", Body("nobody@x.com|2026-11-14|09:00|10:00"));

            await AssertResponse(response, HttpStatusCode.BadRequest, null);
        }
    }
}
