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
    public async Task Get_Plan_By_Id_Returns_Plan_Or_NotFound()
    {
        var okRes = await _client.GetAsync("/api/v1/plans/st-george");
        Assert.Equal(HttpStatusCode.OK, okRes.StatusCode);

        var notFoundRes = await _client.GetAsync("/api/v1/plans/non-existing-plan");
        Assert.Equal(HttpStatusCode.NotFound, notFoundRes.StatusCode);
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

    [Theory]
    [InlineData(null, "monthly")]
    [InlineData("st-george", null)]
    [InlineData("st-george", "invalid-term")]
    public async Task Post_Amortization_Invalid_Payload_Returns_BadRequest(string? planId, string? term)
    {
        var payload = new { planId, paymentTerm = term };
        var response = await _client.PostAsJsonAsync("/api/v1/calculator/amortization", payload);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Auth_Register_And_Login_Flow()
    {
        // 1. Missing fields
        var badReg = await _client.PostAsJsonAsync("/api/v1/auth/register", new { email = "", password = "" });
        Assert.Equal(HttpStatusCode.BadRequest, badReg.StatusCode);

        // 2. Short password
        var shortPass = await _client.PostAsJsonAsync("/api/v1/auth/register", new { email = "test@example.com", password = "short" });
        Assert.Equal(HttpStatusCode.BadRequest, shortPass.StatusCode);

        // 3. Valid register
        var reg = await _client.PostAsJsonAsync("/api/v1/auth/register", new {
            email = "maria.santos@stpeter.com.ph",
            password = "Password123!",
            fullName = "Maria Santos",
            phoneNumber = "+639178821940",
            role = "Planholder"
        });
        Assert.Equal(HttpStatusCode.Created, reg.StatusCode);

        // 4. Missing login fields
        var badLogin = await _client.PostAsJsonAsync("/api/v1/auth/login", new { email = "", password = "" });
        Assert.Equal(HttpStatusCode.BadRequest, badLogin.StatusCode);

        // 5. Valid login
        var login = await _client.PostAsJsonAsync("/api/v1/auth/login", new {
            email = "maria.santos@stpeter.com.ph",
            password = "Password123!"
        });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Fact]
    public async Task Auth_Me_Requires_Bearer_Token()
    {
        var unauth = await _client.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, unauth.StatusCode);

        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        req.Headers.Add("Authorization", "Bearer stp-token-12345");
        var auth = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.OK, auth.StatusCode);
    }

    [Fact]
    public async Task Enrollment_Draft_And_Beneficiaries_Flow()
    {
        // Bad draft
        var badDraft = await _client.PostAsJsonAsync("/api/v1/enrollment/draft", new { planId = "", paymentTerm = "" });
        Assert.Equal(HttpStatusCode.BadRequest, badDraft.StatusCode);

        // Bad term draft
        var badTermDraft = await _client.PostAsJsonAsync("/api/v1/enrollment/draft", new { planId = "st-george", paymentTerm = "invalid" });
        Assert.Equal(HttpStatusCode.BadRequest, badTermDraft.StatusCode);

        // Valid draft
        var validDraft = await _client.PostAsJsonAsync("/api/v1/enrollment/draft", new {
            planId = "st-gregory",
            paymentTerm = "monthly",
            assigneeType = "LovedOne",
            assigneeName = "Rosa Santos",
            assigneeDob = DateTime.UtcNow.AddYears(-64),
            assigneeRelationship = "Mother"
        });
        Assert.Equal(HttpStatusCode.Created, validDraft.StatusCode);

        // Attach beneficiaries - valid
        var validBens = await _client.PostAsJsonAsync("/api/v1/enrollment/enr-001/beneficiaries", new {
            beneficiaries = new[] {
                new { fullName = "Maria Santos", relationship = "Daughter", percentageShare = 100m, isPrimary = true }
            }
        });
        Assert.Equal(HttpStatusCode.OK, validBens.StatusCode);

        // Attach beneficiaries - invalid sum
        var invalidBens = await _client.PostAsJsonAsync("/api/v1/enrollment/enr-001/beneficiaries", new {
            beneficiaries = new[] {
                new { fullName = "Maria Santos", relationship = "Daughter", percentageShare = 90m, isPrimary = true }
            }
        });
        Assert.Equal(HttpStatusCode.BadRequest, invalidBens.StatusCode);
    }

    [Fact]
    public async Task Checkout_Intent_And_Verification_Flow()
    {
        // Create intent with valid agent
        var intent = await _client.PostAsJsonAsync("/api/v1/checkout/create-intent", new {
            enrollmentId = "enr-draft-123",
            paymentMethod = "gcash",
            agentCode = "AGNT-7821-MNL"
        });
        Assert.Equal(HttpStatusCode.OK, intent.StatusCode);

        // Verify payment - missing ref
        var badVerify = await _client.PostAsJsonAsync("/api/v1/checkout/verify-payment", new { transactionRef = "" });
        Assert.Equal(HttpStatusCode.BadRequest, badVerify.StatusCode);

        // Verify payment - valid
        var validVerify = await _client.PostAsJsonAsync("/api/v1/checkout/verify-payment", new { transactionRef = "TXN-GCASH-12345" });
        Assert.Equal(HttpStatusCode.OK, validVerify.StatusCode);
    }

    [Fact]
    public async Task Dashboard_Endpoints_Flow()
    {
        var nextDue = await _client.GetAsync("/api/v1/dashboard/installments/next-due");
        Assert.Equal(HttpStatusCode.OK, nextDue.StatusCode);

        var pay = await _client.PostAsJsonAsync("/api/v1/dashboard/installments/pay", new {
            contractNumber = "STP-2026-88421",
            monthsToPay = 2,
            paymentMethod = "Maya"
        });
        Assert.Equal(HttpStatusCode.OK, pay.StatusCode);

        // Pay 0 months defaults to 1
        var payZero = await _client.PostAsJsonAsync("/api/v1/dashboard/installments/pay", new {
            contractNumber = "STP-2026-88421",
            monthsToPay = 0,
            paymentMethod = "GCash"
        });
        Assert.Equal(HttpStatusCode.OK, payZero.StatusCode);
    }

    [Fact]
    public async Task Chapels_Filters_Flow()
    {
        var all = await _client.GetAsync("/api/v1/chapels");
        Assert.Equal(HttpStatusCode.OK, all.StatusCode);

        var cremTrue = await _client.GetAsync("/api/v1/chapels?hasCrematorium=true");
        Assert.Equal(HttpStatusCode.OK, cremTrue.StatusCode);

        var cremFalse = await _client.GetAsync("/api/v1/chapels?hasCrematorium=false");
        Assert.Equal(HttpStatusCode.OK, cremFalse.StatusCode);
    }

    [Fact]
    public async Task EBurol_Candle_And_Tributes_Flow()
    {
        // Session not found
        var notFound = await _client.GetAsync("/api/v1/e-burol/session/NON-EXISTENT?pin=123456");
        Assert.Equal(HttpStatusCode.Forbidden, notFound.StatusCode);

        // Candle not found
        var badCandle = await _client.PostAsync("/api/v1/e-burol/session/NON-EXISTENT/candle", null);
        Assert.Equal(HttpStatusCode.NotFound, badCandle.StatusCode);

        // Candle increment valid
        var candle = await _client.PostAsync("/api/v1/e-burol/session/EBR-402/candle", null);
        Assert.Equal(HttpStatusCode.OK, candle.StatusCode);

        // Tribute bad request
        var badTribute = await _client.PostAsJsonAsync("/api/v1/e-burol/session/EBR-402/tribute", new { senderName = "", message = "" });
        Assert.Equal(HttpStatusCode.BadRequest, badTribute.StatusCode);

        // Tribute valid
        var tribute = await _client.PostAsJsonAsync("/api/v1/e-burol/session/EBR-402/tribute", new {
            senderName = "Paolo Santos",
            message = "Rest in peace",
            relation = "Nephew"
        });
        Assert.Equal(HttpStatusCode.Created, tribute.StatusCode);
    }
}
