using EcommerceStpeterv2Backend.Domain;
using Xunit;

namespace EcommerceStpeterv2Backend.Tests;

public class BeneficiaryValidatorTests
{
    [Fact]
    public void Sum_Equals_100_Is_Valid()
    {
        var beneficiaries = new List<BeneficiaryInput>
        {
            new("Juan Cruz", "Son", DateTime.UtcNow.AddYears(-20), 60m, true),
            new("Ana Cruz", "Daughter", DateTime.UtcNow.AddYears(-18), 40m, true)
        };

        var (isValid, errorMessage) = BeneficiaryValidator.Validate(beneficiaries);

        Assert.True(isValid);
        Assert.Null(errorMessage);
    }

    [Fact]
    public void Single_Beneficiary_100_Is_Valid()
    {
        var beneficiaries = new List<BeneficiaryInput>
        {
            new("Rosa Santos", "Mother", DateTime.UtcNow.AddYears(-60), 100m, true)
        };

        var (isValid, errorMessage) = BeneficiaryValidator.Validate(beneficiaries);

        Assert.True(isValid);
        Assert.Null(errorMessage);
    }

    [Fact]
    public void Sum_Exceeding_100_Is_Invalid()
    {
        var beneficiaries = new List<BeneficiaryInput>
        {
            new("Juan Cruz", "Son", DateTime.UtcNow.AddYears(-20), 70m, true),
            new("Ana Cruz", "Daughter", DateTime.UtcNow.AddYears(-18), 40m, true)
        };

        var (isValid, errorMessage) = BeneficiaryValidator.Validate(beneficiaries);

        Assert.False(isValid);
        Assert.Contains("110%", errorMessage);
    }

    [Fact]
    public void Sum_Less_Than_100_Is_Invalid()
    {
        var beneficiaries = new List<BeneficiaryInput>
        {
            new("Juan Cruz", "Son", DateTime.UtcNow.AddYears(-20), 50m, true),
            new("Ana Cruz", "Daughter", DateTime.UtcNow.AddYears(-18), 49m, true)
        };

        var (isValid, errorMessage) = BeneficiaryValidator.Validate(beneficiaries);

        Assert.False(isValid);
        Assert.Contains("99%", errorMessage);
    }

    [Fact]
    public void Empty_Beneficiaries_Returns_Invalid()
    {
        var (isValid, errorMessage) = BeneficiaryValidator.Validate(new List<BeneficiaryInput>());
        Assert.False(isValid);
        Assert.NotNull(errorMessage);
    }

    [Fact]
    public void Missing_FullName_Returns_Invalid()
    {
        var beneficiaries = new List<BeneficiaryInput>
        {
            new("", "Son", DateTime.UtcNow.AddYears(-20), 100m, true)
        };

        var (isValid, errorMessage) = BeneficiaryValidator.Validate(beneficiaries);
        Assert.False(isValid);
        Assert.Contains("full name", errorMessage);
    }

    [Fact]
    public void Zero_Or_Negative_Percentage_Returns_Invalid()
    {
        var beneficiaries = new List<BeneficiaryInput>
        {
            new("Juan Cruz", "Son", DateTime.UtcNow.AddYears(-20), 0m, true)
        };

        var (isValid, errorMessage) = BeneficiaryValidator.Validate(beneficiaries);
        Assert.False(isValid);
    }
}
