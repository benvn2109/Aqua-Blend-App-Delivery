using System.Net;
using System.Net.Http.Json;
using AquaBlend.DTOs.WaterSources;

namespace AquaBlend.Tests;

public class WaterSourceEndpointsTests : IDisposable
{
    private readonly AquaBlendApiFactory _factory;
    private readonly HttpClient _client;

    public WaterSourceEndpointsTests()
    {
        _factory = new AquaBlendApiFactory();
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task GetAll_ReturnsWaterSources()
    {
        var response = await _client.GetAsync("/api/water-sources");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var waterSources = await response.Content.ReadFromJsonAsync<List<WaterSourceResponseDto>>();

        Assert.NotNull(waterSources);
        Assert.NotEmpty(waterSources!);
        Assert.Contains(waterSources!, w => w.Name == "Reservoir A");
    }

    [Fact]
    public async Task GetById_ReturnsWaterSource()
    {
        var listResponse = await _client.GetAsync("/api/water-sources");
        listResponse.EnsureSuccessStatusCode();
        var waterSources = await listResponse.Content.ReadFromJsonAsync<List<WaterSourceResponseDto>>();

        Assert.NotNull(waterSources);
        var existing = waterSources!.First();

        var response = await _client.GetAsync($"/api/water-sources/{existing.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var waterSource = await response.Content.ReadFromJsonAsync<WaterSourceResponseDto>();
        Assert.NotNull(waterSource);
        Assert.Equal(existing.Id, waterSource!.Id);
        Assert.Equal(existing.Name, waterSource.Name);
        Assert.Equal(existing.Type, waterSource.Type);
    }

    [Fact]
    public async Task GetById_UnknownId_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/water-sources/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_ReturnsCreatedWaterSource()
    {
        var dto = new CreateWaterSourceDto
        {
            Name = "New Water Source",
            Type = "Reservoir"
        };

        var response = await _client.PostAsJsonAsync("/api/water-sources", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var created = await response.Content.ReadFromJsonAsync<WaterSourceResponseDto>();

        Assert.NotNull(created);
        Assert.Equal(dto.Name, created!.Name);
        Assert.Equal(dto.Type, created.Type);
        Assert.True(created.Id > 0);
        Assert.NotEqual(default, created.CreatedAt);
    }

    [Fact]
    public async Task Create_InvalidData_ReturnsBadRequest()
    {
        var invalidDto = new CreateWaterSourceDto
        {
            Name = string.Empty,
            Type = string.Empty
        };

        var response = await _client.PostAsJsonAsync("/api/water-sources", invalidDto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WhitespaceOnlyName_ReturnsBadRequest()
    {
        // Regression test for the reviewer-flagged gap: [Required] alone let
        // whitespace-only values through, which .Trim() would then silently
        // reduce to an empty string before saving.
        var whitespaceDto = new CreateWaterSourceDto
        {
            Name = "   ",
            Type = "Reservoir"
        };

        var response = await _client.PostAsJsonAsync("/api/water-sources", whitespaceDto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_ExistingWaterSource_ReturnsNoContent()
    {
        var dto = new CreateWaterSourceDto
        {
            Name = "Update Test Source",
            Type = "Reservoir"
        };

        var createResponse = await _client.PostAsJsonAsync("/api/water-sources", dto);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<WaterSourceResponseDto>();
        Assert.NotNull(created);

        var updateDto = new UpdateWaterSourceDto
        {
            Name = "Updated Source Name",
            Type = "Groundwater"
        };

        var updateResponse = await _client.PutAsJsonAsync($"/api/water-sources/{created!.Id}", updateDto);

        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/water-sources/{created.Id}");
        getResponse.EnsureSuccessStatusCode();
        var updated = await getResponse.Content.ReadFromJsonAsync<WaterSourceResponseDto>();

        Assert.NotNull(updated);
        Assert.Equal(updateDto.Name, updated!.Name);
        Assert.Equal(updateDto.Type, updated.Type);
    }

    [Fact]
    public async Task Update_UnknownId_ReturnsNotFound()
    {
        var updateDto = new UpdateWaterSourceDto
        {
            Name = "Missing Source",
            Type = "Reservoir"
        };

        var response = await _client.PutAsJsonAsync("/api/water-sources/999999", updateDto);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ExistingWaterSource_RemovesWaterSource()
    {
        var dto = new CreateWaterSourceDto
        {
            Name = "Delete Test Source",
            Type = "Reservoir"
        };

        var createResponse = await _client.PostAsJsonAsync("/api/water-sources", dto);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<WaterSourceResponseDto>();
        Assert.NotNull(created);

        var deleteResponse = await _client.DeleteAsync($"/api/water-sources/{created!.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/water-sources/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_UnknownId_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync("/api/water-sources/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
