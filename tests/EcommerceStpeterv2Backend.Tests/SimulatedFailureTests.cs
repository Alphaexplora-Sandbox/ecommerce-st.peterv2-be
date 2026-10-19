using EcommerceStpeterv2Backend.Domain;
using Xunit;

namespace EcommerceStpeterv2Backend.Tests;

/// <summary>
/// CI/CD ERROR SIMULATION TEST SUITE
///
/// Objective:
/// Demonstrates an intentional test case failure caught by the automated pipeline
/// (Stage 10: ALPHACI 2 Quality -> dotnet-tests).
///
/// FAILURE SCENARIO:
/// An intentional test defect asserting that an invalid 110% beneficiary allocation
/// should pass validation.
///
/// CURRENT STATUS: FAILING (Simulated Error Active)
///
/// TO APPLY THE FIX:
/// Change SimulationFixed to true, which asserts that isValid == false.
/// </summary>
public class SimulatedFailureTests
{
    public const bool SimulationFixed = false;

    [Fact]
    public void TC_SIM_001_Fails_On_Invalid_Beneficiary_Sum_Assertion()
    {
        var beneficiaries = new List<BeneficiaryInput>
        {
            new("Juan Cruz", "Son", DateTime.UtcNow.AddYears(-20), 70m, true),
            new("Ana Cruz", "Daughter", DateTime.UtcNow.AddYears(-18), 40m, true)
        };

        var (isValid, errorMessage) = BeneficiaryValidator.Validate(beneficiaries);

        if (!SimulationFixed)
        {
            // INTENTIONAL FAILURE: Expecting 110% allocation to be valid
            Assert.True(isValid, "Simulated Failure: Expected 110% allocation to pass validation, but it was correctly rejected: " + errorMessage);
        }
        else
        {
            // THE FIX: Correctly asserts that 110% allocation is invalid
            Assert.False(isValid);
            Assert.Contains("110%", errorMessage);
        }
    }
}
