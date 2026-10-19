using EcommerceStpeterv2Backend.Domain;
using Xunit;

namespace EcommerceStpeterv2Backend.Tests;

public class AgentValidatorTests
{
    [Theory]
    [InlineData("AGNT-7821-MNL", "Jessica Bautista", "Manila Central Branch")]
    [InlineData("AGNT-5501-CEB", "Marco Tan", "Cebu Regional Branch")]
    [InlineData("AGNT-9902-DVO", "Sarah Lim", "Davao Southern Branch")]
    public void Valid_Agent_Code_Resolves_Details(string code, string expectedName, string expectedBranch)
    {
        var agent = AgentValidator.Validate(code);

        Assert.True(agent.IsValid);
        Assert.Equal(expectedName, agent.AgentName);
        Assert.Equal(expectedBranch, agent.Branch);
    }

    [Theory]
    [InlineData("FAKE-9999")]
    [InlineData("INVALID-CODE")]
    [InlineData("")]
    [InlineData(null)]
    public void Invalid_Agent_Code_Returns_Not_Valid(string? code)
    {
        var agent = AgentValidator.Validate(code);
        Assert.False(agent.IsValid);
    }
}
