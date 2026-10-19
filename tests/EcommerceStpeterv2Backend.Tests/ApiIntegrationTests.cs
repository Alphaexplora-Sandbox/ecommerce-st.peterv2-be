using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EcommerceStpeterv2Backend.Domain;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace EcommerceStpeterv2Backend.Tests;

public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_Plans_Returns_Catalog()
    {
        var response = await _client.GetAsync("/api/v1/plans");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var plans = await response.Content.ReadFromJsonAsync<List<Plan>>();
        Assert.NotNull(plans);
        Assert.True(plans!.Count >= 6);
    }

    [Fact]
    public async Task Get_Plans_Filter_By_Category_Returns_Only_Category()
    {
        var response = await _client.GetAsync("/api/v1/plans?category=Cremation");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var plans = await response.Content.ReadFromJsonAsync<List<Plan>>();
        Assert.NotNull(plans);
        Assert.All(plans!, p => Assert.Equal("Cremation", p.Category));
    }

    [Fact]
    public async Task Post_Amortization_SpotCash_Calculates_Discount()
    {
        var payload = new { planId = "st-george", paymentTerm = "spotcash" };
        var response = await _client.PostAsJsonAsync("/api/v1/calculator/amortization", payload);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<AmortizationResult>();
        Assert.NotNull(result);
        Assert.Equal(148500m, result!.NetContractPrice);
        Assert.Equal(16500m, result.DiscountAmount);
    }

    [Fact]
    public async Task Post_Amortization_Null_Field_Returns_BadRequest()
    {
        var payload = new { planId = "st-george", paymentTerm = (string?)null };
        var response = await _client.PostAsJsonAsync("/api/v1/calculator/amortization", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_Beneficiary_Validation_100_Returns_Ok()
    {
        var payload = new
        {
            beneficiaries = new[]
            {
                new { fullName = "Maria Santos", relationship = "Daughter", percentageShare = 70m, isPrimary = true },
                new { fullName = "Paolo Santos", relationship = "Son", percentageShare = 30m, isPrimary = true }
            }
        };

        var response = await _client.PostAsJsonAsync("/api/v1/enrollment/validate-beneficiaries", payload);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Post_Beneficiary_Validation_110_Returns_BadRequest()
    {
        var payload = new
        {
            beneficiaries = new[]
            {
                new { fullName = "Maria Santos", relationship = "Daughter", percentageShare = 70m, isPrimary = true },
                new { fullName = "Paolo Santos", relationship = "Son", percentageShare = 40m, isPrimary = true }
            }
        };

        var response = await _client.PostAsJsonAsync("/api/v1/enrollment/validate-beneficiaries", payload);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Dashboard_Plans_Without_Auth_Returns_Unauthorized()
    {
        var response = await _client.GetAsync("/api/v1/dashboard/plans");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Dashboard_Plans_With_Auth_Returns_Ok()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/dashboard/plans");
        request.Headers.Add("Authorization", "Bearer mock-test-token");
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Chapels_Returns_Directory()
    {
        var response = await _client.GetAsync("/api/v1/chapels?region=NCR");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var chapels = await response.Content.ReadFromJsonAsync<List<Chapel>>();
        Assert.NotNull(chapels);
        Assert.NotEmpty(chapels!);
    }

    [Fact]
    public async Task EBurol_Requires_Valid_Pin()
    {
        var failRes = await _client.GetAsync("/api/v1/e-burol/session/EBR-402?pin=wrong");
        Assert.Equal(HttpStatusCode.Forbidden, failRes.StatusCode);

        var successRes = await _client.GetAsync("/api/v1/e-burol/session/EBR-402?pin=882194");
        Assert.Equal(HttpStatusCode.OK, successRes.StatusCode);
    }
}