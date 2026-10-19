using EcommerceStpeterv2Backend.Domain;
using Xunit;

namespace EcommerceStpeterv2Backend.Tests;

public class EnrollmentPolicyTests
{
    [Fact]
    public void Age_Under_60_Qualifies_For_Insurance_Rider()
    {
        var dob = DateTime.UtcNow.AddYears(-35);
        var (hasRider, notice) = EnrollmentPolicy.EvaluateAgeEligibility(dob);

        Assert.True(hasRider);
        Assert.Contains("Eligible for full life insurance", notice);
    }

    [Fact]
    public void Age_Over_60_Waives_Insurance_Rider()
    {
        var dob = DateTime.UtcNow.AddYears(-64);
        var (hasRider, notice) = EnrollmentPolicy.EvaluateAgeEligibility(dob);

        Assert.False(hasRider);
        Assert.Contains("Insurance rider waived", notice);
    }

    [Fact]
    public void Generated_Contract_Number_Follows_Pattern()
    {
        var contractNum = EnrollmentPolicy.GenerateContractNumber();

        Assert.StartsWith("STP-2026-", contractNum);
        Assert.Equal(14, contractNum.Length);
    }
}
