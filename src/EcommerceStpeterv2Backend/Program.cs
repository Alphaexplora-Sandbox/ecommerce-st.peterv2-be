using System.Text.Json.Serialization;
using EcommerceStpeterv2Backend.Domain;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IServiceStatus, ServiceStatus>();
builder.Services.AddOpenApi();

var corsOrigins = (builder.Configuration["CORS_ORIGINS"] ?? "http://localhost:3000")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins(corsOrigins)
          .AllowAnyHeader()
          .AllowAnyMethod()));

var app = builder.Build();

app.UseCors();
app.MapOpenApi("/openapi/v1.json");

// Health and Root
app.MapGet("/health", (IServiceStatus status) =>
    Results.Ok(new HealthResponse(status.CurrentStatus(), ServiceInfo.Name)))
   .Produces<HealthResponse>(StatusCodes.Status200OK)
   .WithName("HealthCheck");

app.MapGet("/", () => Results.Ok(new HealthResponse("ready", ServiceInfo.Name)))
   .Produces<HealthResponse>(StatusCodes.Status200OK)
   .WithName("RootStatus");

// Authentication Endpoints
app.MapPost("/api/v1/auth/register", (RegisterRequest req) =>
{
    if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
    {
        return Results.BadRequest(new { error = "Email and password are required." });
    }

    if (req.Password.Length < 8)
    {
        return Results.BadRequest(new { error = "Password must be at least 8 characters long." });
    }

    var userId = Guid.NewGuid().ToString();
    var user = new User(
        userId,
        req.Email.Trim().ToLowerInvariant(),
        "hashed_" + req.Password.Length,
        req.FullName?.Trim() ?? "Valued Planholder",
        req.PhoneNumber?.Trim() ?? "+639170000000",
        req.Role?.Trim() ?? "Planholder",
        DateTime.UtcNow
    );

    var token = "stp-jwt-" + Guid.NewGuid().ToString("N");
    return Results.Created($"/api/v1/users/{user.Id}", new
    {
        token,
        tokenType = "Bearer",
        expiresIn = 3600,
        user = new
        {
            user.Id,
            user.Email,
            user.FullName,
            user.PhoneNumber,
            user.Role
        }
    });
}).Produces(StatusCodes.Status201Created).Produces(StatusCodes.Status400BadRequest);

app.MapPost("/api/v1/auth/login", (LoginRequest req) =>
{
    if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
    {
        return Results.BadRequest(new { error = "Email and password are required." });
    }

    var email = req.Email.Trim().ToLowerInvariant();
    var token = "stp-jwt-" + Guid.NewGuid().ToString("N");
    var fullName = email.StartsWith("maria") ? "Maria Santos" : "Ramon Cruz";

    return Results.Ok(new
    {
        token,
        tokenType = "Bearer",
        expiresIn = 3600,
        user = new
        {
            id = Guid.NewGuid().ToString(),
            email,
            fullName,
            role = "Planholder"
        }
    });
}).Produces(StatusCodes.Status200OK).Produces(StatusCodes.Status400BadRequest);

app.MapGet("/api/v1/auth/me", (HttpRequest httpRequest) =>
{
    var authHeader = httpRequest.Headers.Authorization.ToString();
    if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
    {
        return Results.Unauthorized();
    }

    return Results.Ok(new
    {
        id = "usr-88421",
        email = "maria.santos@example.com",
        fullName = "Maria Santos",
        phoneNumber = "+639178821940",
        role = "Planholder",
        activePlansCount = 1
    });
}).Produces(StatusCodes.Status200OK).Produces(StatusCodes.Status401Unauthorized);

// Catalog Endpoints
app.MapGet("/api/v1/plans", ([FromQuery] string? category) =>
{
    var plans = MemorialCatalog.AllPlans;
    if (!string.IsNullOrWhiteSpace(category))
    {
        plans = plans.Where(p => string.Equals(p.Category, category.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
    }
    return Results.Ok(plans);
}).Produces<IEnumerable<Plan>>(StatusCodes.Status200OK);

app.MapGet("/api/v1/plans/{id}", (string id) =>
{
    var plan = MemorialCatalog.AllPlans.FirstOrDefault(p => string.Equals(p.Id, id.Trim(), StringComparison.OrdinalIgnoreCase));
    return plan != null ? Results.Ok(plan) : Results.NotFound(new { error = $"Plan '{id}' not found." });
}).Produces<Plan>(StatusCodes.Status200OK).Produces(StatusCodes.Status404NotFound);

// Calculator Endpoints
app.MapPost("/api/v1/calculator/amortization", (AmortizationRequest req) =>
{
    if (string.IsNullOrWhiteSpace(req.PlanId))
    {
        return Results.BadRequest(new { error = "planId is required." });
    }
    if (string.IsNullOrWhiteSpace(req.PaymentTerm))
    {
        return Results.BadRequest(new { error = "paymentTerm is required." });
    }

    try
    {
        var result = AmortizationCalculator.Calculate(req.PlanId, req.PaymentTerm, req.CustomBasePrice);
        return Results.Ok(result);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
}).Produces<AmortizationResult>(StatusCodes.Status200OK).Produces(StatusCodes.Status400BadRequest);

// Enrollment & Beneficiary Endpoints
app.MapPost("/api/v1/enrollment/validate-beneficiaries", (BeneficiaryValidationRequest req) =>
{
    var (isValid, errorMessage) = BeneficiaryValidator.Validate(req.Beneficiaries);
    if (!isValid)
    {
        var total = req.Beneficiaries?.Where(b => b.IsPrimary).Sum(b => b.PercentageShare) ?? 0m;
        return Results.BadRequest(new { valid = false, error = errorMessage, totalPercentage = total });
    }

    return Results.Ok(new { valid = true, totalPercentage = 100m, message = "Total 100% Allocated" });
}).Produces(StatusCodes.Status200OK).Produces(StatusCodes.Status400BadRequest);

app.MapPost("/api/v1/enrollment/draft", (EnrollmentDraftRequest req) =>
{
    if (string.IsNullOrWhiteSpace(req.PlanId) || string.IsNullOrWhiteSpace(req.PaymentTerm))
    {
        return Results.BadRequest(new { error = "planId and paymentTerm are required." });
    }

    AmortizationResult amort;
    try
    {
        amort = AmortizationCalculator.Calculate(req.PlanId, req.PaymentTerm);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }

    var dob = req.AssigneeDob ?? DateTime.UtcNow.AddYears(-35);
    var (hasRider, policyNotice) = EnrollmentPolicy.EvaluateAgeEligibility(dob);
    var contractNumber = EnrollmentPolicy.GenerateContractNumber();
    var enrollmentId = Guid.NewGuid().ToString();

    var enrollment = new Enrollment(
        enrollmentId,
        req.UserId ?? "usr-guest",
        amort.PlanId,
        contractNumber,
        req.AssigneeType ?? "Self",
        req.AssigneeName ?? "Principal Planholder",
        dob,
        req.AssigneeRelationship ?? "Self",
        amort.PaymentTerm,
        amort.BasePrice,
        amort.DiscountAmount,
        amort.NetContractPrice,
        amort.InstallmentAmount,
        amort.TotalInstallments,
        0,
        "Draft",
        hasRider,
        DateTime.UtcNow.AddMonths(1),
        DateTime.UtcNow
    );

    return Results.Created($"/api/v1/enrollment/{enrollment.Id}", new
    {
        enrollment,
        policyNotice
    });
}).Produces(StatusCodes.Status201Created).Produces(StatusCodes.Status400BadRequest);

app.MapPost("/api/v1/enrollment/{id}/beneficiaries", (string id, BeneficiaryValidationRequest req) =>
{
    var (isValid, errorMessage) = BeneficiaryValidator.Validate(req.Beneficiaries);
    if (!isValid)
    {
        return Results.BadRequest(new { error = errorMessage });
    }

    return Results.Ok(new
    {
        enrollmentId = id,
        beneficiariesCount = req.Beneficiaries?.Count ?? 0,
        status = "BeneficiariesAssigned"
    });
}).Produces(StatusCodes.Status200OK).Produces(StatusCodes.Status400BadRequest);

// Checkout Endpoints
app.MapPost("/api/v1/checkout/create-intent", (CheckoutIntentRequest req) =>
{
    var method = string.IsNullOrWhiteSpace(req.PaymentMethod) ? "GCASH" : req.PaymentMethod.Trim().ToUpperInvariant();
    var txnRef = $"TXN-{method}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
    var agent = AgentValidator.Validate(req.AgentCode);

    return Results.Ok(new
    {
        transactionRef = txnRef,
        enrollmentId = req.EnrollmentId ?? "enr-draft-001",
        paymentMethod = method,
        paymentUrl = $"https://pay.stpeter.com.ph/checkout/{txnRef}",
        agent = agent.IsValid ? agent : null,
        status = "Pending"
    });
}).Produces(StatusCodes.Status200OK);

app.MapPost("/api/v1/checkout/verify-payment", (VerifyPaymentRequest req) =>
{
    if (string.IsNullOrWhiteSpace(req.TransactionRef))
    {
        return Results.BadRequest(new { error = "transactionRef is required." });
    }

    var contractNum = "STP-2026-88421";
    return Results.Ok(new
    {
        transactionRef = req.TransactionRef,
        contractNumber = contractNum,
        status = "Active",
        message = $"Congratulations! Policy {contractNum} is now officially active.",
        certificateUrl = $"/api/v1/contracts/{contractNum}/certificate.pdf",
        receiptUrl = $"/api/v1/contracts/{contractNum}/receipt.pdf"
    });
}).Produces(StatusCodes.Status200OK).Produces(StatusCodes.Status400BadRequest);

// Dashboard Endpoints
app.MapGet("/api/v1/dashboard/plans", (HttpRequest httpRequest) =>
{
    var authHeader = httpRequest.Headers.Authorization.ToString();
    if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
    {
        return Results.Unauthorized();
    }

    var userPlans = new[]
    {
        new
        {
            contractNumber = "STP-2026-88421",
            planId = "st-gregory",
            planName = "St. Gregory (Hardwood Casket)",
            assigneeName = "Rosa Santos",
            paymentTerm = "Monthly (5 Years)",
            totalContractPrice = 75000m,
            installmentAmount = 1250m,
            installmentsPaid = 14,
            totalInstallments = 60,
            completionPercentage = 23,
            standing = "Active",
            nextDueDate = DateTime.UtcNow.AddDays(15).ToString("yyyy-MM-dd"),
            hasInsuranceRider = false
        }
    };

    return Results.Ok(new { plans = userPlans });
}).Produces(StatusCodes.Status200OK).Produces(StatusCodes.Status401Unauthorized);

app.MapGet("/api/v1/dashboard/installments/next-due", () =>
{
    return Results.Ok(new
    {
        contractNumber = "STP-2026-88421",
        installmentNumber = 15,
        totalInstallments = 60,
        amountDue = 1250m,
        dueDate = DateTime.UtcNow.AddDays(15).ToString("yyyy-MM-dd"),
        gracePeriodEnds = DateTime.UtcNow.AddDays(45).ToString("yyyy-MM-dd")
    });
}).Produces(StatusCodes.Status200OK);

app.MapPost("/api/v1/dashboard/installments/pay", (PayInstallmentRequest req) =>
{
    var months = req.MonthsToPay <= 0 ? 1 : req.MonthsToPay;
    var baseInstallment = 1250m;
    var totalPaid = baseInstallment * months;

    return Results.Ok(new
    {
        contractNumber = req.ContractNumber ?? "STP-2026-88421",
        monthsPaid = months,
        amountPaid = totalPaid,
        newInstallmentCount = 14 + months,
        totalInstallments = 60,
        status = "Success",
        receiptReference = $"OR-STP-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}"
    });
}).Produces(StatusCodes.Status200OK);

// Chapels Directory Endpoints
app.MapGet("/api/v1/chapels", ([FromQuery] string? region, [FromQuery] bool? hasCrematorium) =>
{
    var list = ChapelDirectory.AllChapels.AsEnumerable();
    if (!string.IsNullOrWhiteSpace(region))
    {
        list = list.Where(c => string.Equals(c.Region, region.Trim(), StringComparison.OrdinalIgnoreCase));
    }
    if (hasCrematorium.HasValue)
    {
        list = list.Where(c => c.HasCrematorium == hasCrematorium.Value);
    }
    return Results.Ok(list.ToList());
}).Produces<IEnumerable<Chapel>>(StatusCodes.Status200OK);

// E-Burol Virtual Wake Hub Endpoints
app.MapGet("/api/v1/e-burol/session/{id}", (string id, [FromQuery] string? pin) =>
{
    var (success, errorMessage, session) = EBurolStore.AccessSession(id, pin);
    if (!success || session == null)
    {
        return Results.Json(new { error = errorMessage ?? "Access denied." }, statusCode: StatusCodes.Status403Forbidden);
    }

    var tributes = EBurolStore.GetTributes(id);
    return Results.Ok(new
    {
        session,
        tributes
    });
}).Produces(StatusCodes.Status200OK).Produces(StatusCodes.Status403Forbidden);

app.MapPost("/api/v1/e-burol/session/{id}/candle", (string id) =>
{
    var newCount = EBurolStore.IncrementCandle(id);
    if (newCount == 0)
    {
        return Results.NotFound(new { error = $"Session '{id}' not found." });
    }
    return Results.Ok(new { sessionId = id, candleCount = newCount });
}).Produces(StatusCodes.Status200OK).Produces(StatusCodes.Status404NotFound);

app.MapPost("/api/v1/e-burol/session/{id}/tribute", (string id, TributeRequest req) =>
{
    if (string.IsNullOrWhiteSpace(req.SenderName) || string.IsNullOrWhiteSpace(req.Message))
    {
        return Results.BadRequest(new { error = "senderName and message are required." });
    }

    var tribute = EBurolStore.AddTribute(id, req.SenderName.Trim(), req.Message.Trim(), req.Relation?.Trim() ?? "Friend/Family");
    return Results.Created($"/api/v1/e-burol/session/{id}/tribute/{tribute.Id}", tribute);
}).Produces<TributeMessage>(StatusCodes.Status201Created).Produces(StatusCodes.Status400BadRequest);

app.Run();

// DTOs & Records
public record HealthResponse(string Status, string Service);
public record RegisterRequest(string? Email, string? Password, string? FullName, string? PhoneNumber, string? Role);
public record LoginRequest(string? Email, string? Password);
public record AmortizationRequest(string? PlanId, string? PaymentTerm, decimal? CustomBasePrice = null);
public record BeneficiaryValidationRequest(List<BeneficiaryInput>? Beneficiaries);
public record EnrollmentDraftRequest(string? PlanId, string? AssigneeType, string? AssigneeName, DateTime? AssigneeDob, string? AssigneeRelationship, string? PaymentTerm, string? UserId = null);
public record CheckoutIntentRequest(string? EnrollmentId, string? PaymentMethod, string? AgentCode);
public record VerifyPaymentRequest(string? TransactionRef, string? GatewayStatus);
public record PayInstallmentRequest(string? ContractNumber, int MonthsToPay, string? PaymentMethod);
public record TributeRequest(string? SenderName, string? Message, string? Relation);

public static class ServiceInfo
{
    public const string Name = "ecommerce-stpeterv2-backend";
}

public partial class Program;