using EcommerceStpeterv2Backend.Domain;
using Xunit;

namespace EcommerceStpeterv2Backend.Tests;

public class AmortizationCalculatorTests
{
    [Theory]
    [InlineData("st-george", 165000, 16500, 148500)]
    [InlineData("st-bernadette", 110000, 11000, 99000)]
    [InlineData("st-gregory", 75000, 7500, 67500)]
    [InlineData("st-claire", 52000, 5200, 46800)]
    [InlineData("st-jude", 98000, 9800, 88200)]
    [InlineData("st-james", 62000, 6200, 55800)]
    public void SpotCash_Deducts_Ten_Percent_Discount(string planId, decimal basePrice, decimal expectedDiscount, decimal expectedNet)
    {
        var result = AmortizationCalculator.Calculate(planId, "spotcash");

        Assert.Equal(basePrice, result.BasePrice);
        Assert.Equal(expectedDiscount, result.DiscountAmount);
        Assert.Equal(expectedNet, result.NetContractPrice);
        Assert.Equal(1, result.TotalInstallments);
        Assert.Equal(expectedNet, result.InstallmentAmount);
        Assert.Contains("Saved", result.SavingsMessage);
    }

    [Fact]
    public void Monthly_Installment_Divides_By_60()
    {
        var result = AmortizationCalculator.Calculate("st-jude", "monthly");

        Assert.Equal(98000m, result.BasePrice);
        Assert.Equal(60, result.TotalInstallments);
        Assert.Equal(1633m, result.InstallmentAmount);
        Assert.Equal(0m, result.DiscountAmount);
    }

    [Fact]
    public void Annual_Installment_Divides_By_5()
    {
        var result = AmortizationCalculator.Calculate("st-george", "annual");

        Assert.Equal(165000m, result.BasePrice);
        Assert.Equal(5, result.TotalInstallments);
        Assert.Equal(33000m, result.InstallmentAmount);
    }

    [Fact]
    public void SemiAnnual_Installment_Divides_By_10()
    {
        var result = AmortizationCalculator.Calculate("st-george", "semiannual");

        Assert.Equal(10, result.TotalInstallments);
        Assert.Equal(16500m, result.InstallmentAmount);
    }

    [Fact]
    public void Quarterly_Installment_Divides_By_20()
    {
        var result = AmortizationCalculator.Calculate("st-george", "quarterly");

        Assert.Equal(20, result.TotalInstallments);
        Assert.Equal(8250m, result.InstallmentAmount);
    }

    [Fact]
    public void Custom_BasePrice_Calculates_Properly()
    {
        var result = AmortizationCalculator.Calculate("custom-pack", "spotcash", 100000m);

        Assert.Equal(100000m, result.BasePrice);
        Assert.Equal(10000m, result.DiscountAmount);
        Assert.Equal(90000m, result.NetContractPrice);
    }

    [Theory]
    [InlineData(null, "monthly")]
    [InlineData("", "monthly")]
    [InlineData("   ", "monthly")]
    public void Null_Or_Empty_PlanId_Throws_ArgumentException(string? planId, string term)
    {
        Assert.Throws<ArgumentException>(() => AmortizationCalculator.Calculate(planId, term));
    }

    [Theory]
    [InlineData("st-george", null)]
    [InlineData("st-george", "")]
    [InlineData("st-george", "invalid-term")]
    public void Invalid_PaymentTerm_Throws_ArgumentException(string planId, string? term)
    {
        Assert.Throws<ArgumentException>(() => AmortizationCalculator.Calculate(planId, term));
    }

    [Fact]
    public void Unknown_PlanId_Throws_ArgumentException()
    {
        Assert.Throws<ArgumentException>(() => AmortizationCalculator.Calculate("unknown-plan-999", "monthly"));
    }
}