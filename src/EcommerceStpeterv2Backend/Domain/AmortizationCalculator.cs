namespace EcommerceStpeterv2Backend.Domain;

public record AmortizationResult(
    string PlanId,
    string PlanName,
    string Category,
    decimal BasePrice,
    string PaymentTerm,
    decimal DiscountAmount,
    decimal NetContractPrice,
    decimal InstallmentAmount,
    int TotalInstallments,
    string SavingsMessage
);

public static class AmortizationCalculator
{
    private static readonly HashSet<string> ValidTerms = new(StringComparer.OrdinalIgnoreCase)
    {
        "spotcash", "annual", "semiannual", "quarterly", "monthly"
    };

    public static AmortizationResult Calculate(string? planId, string? paymentTerm, decimal? customBasePrice = null)
    {
        if (string.IsNullOrWhiteSpace(planId))
        {
            throw new ArgumentException("Plan identifier is required.", nameof(planId));
        }

        if (string.IsNullOrWhiteSpace(paymentTerm))
        {
            throw new ArgumentException("Payment term is required.", nameof(paymentTerm));
        }

        var normalizedTerm = paymentTerm.Trim().ToLowerInvariant();
        if (!ValidTerms.Contains(normalizedTerm))
        {
            throw new ArgumentException($"Invalid payment term '{paymentTerm}'. Allowed: spotcash, annual, semiannual, quarterly, monthly.", nameof(paymentTerm));
        }

        var plan = MemorialCatalog.AllPlans.FirstOrDefault(p => string.Equals(p.Id, planId.Trim(), StringComparison.OrdinalIgnoreCase));
        decimal basePrice;
        string planName;
        string category;

        if (plan != null)
        {
            basePrice = plan.BasePrice;
            planName = plan.Name;
            category = plan.Category;
        }
        else if (customBasePrice.HasValue && customBasePrice.Value > 0)
        {
            basePrice = customBasePrice.Value;
            planName = planId;
            category = "Custom";
        }
        else
        {
            throw new ArgumentException($"Plan with id '{planId}' not found and no valid price provided.", nameof(planId));
        }

        if (basePrice <= 0)
        {
            throw new ArgumentException("Base price must be greater than zero.", nameof(customBasePrice));
        }

        decimal discountAmount = 0m;
        decimal netContractPrice = basePrice;
        decimal installmentAmount;
        int totalInstallments;
        string savingsMessage;

        switch (normalizedTerm)
        {
            case "spotcash":
                discountAmount = Math.Round(basePrice * 0.10m, 2, MidpointRounding.AwayFromZero);
                netContractPrice = basePrice - discountAmount;
                installmentAmount = netContractPrice;
                totalInstallments = 1;
                savingsMessage = $"Saved ₱{discountAmount:N0} (10% Spot Cash Discount)";
                break;

            case "annual":
                totalInstallments = 5;
                installmentAmount = Math.Round(basePrice / 5m, 2, MidpointRounding.AwayFromZero);
                savingsMessage = "5-Year Annual Installment Plan";
                break;

            case "semiannual":
                totalInstallments = 10;
                installmentAmount = Math.Round(basePrice / 10m, 2, MidpointRounding.AwayFromZero);
                savingsMessage = "5-Year Semi-Annual Installment Plan";
                break;

            case "quarterly":
                totalInstallments = 20;
                installmentAmount = Math.Round(basePrice / 20m, 2, MidpointRounding.AwayFromZero);
                savingsMessage = "5-Year Quarterly Installment Plan";
                break;

            case "monthly":
                totalInstallments = 60;
                installmentAmount = Math.Round(basePrice / 60m, MidpointRounding.AwayFromZero);
                savingsMessage = "5-Year Monthly Installment Plan (60 months)";
                break;

            default:
                throw new ArgumentException($"Unsupported payment term: {paymentTerm}");
        }

        return new AmortizationResult(
            planId,
            planName,
            category,
            basePrice,
            normalizedTerm,
            discountAmount,
            netContractPrice,
            installmentAmount,
            totalInstallments,
            savingsMessage
        );
    }
}
