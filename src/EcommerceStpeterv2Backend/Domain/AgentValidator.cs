namespace EcommerceStpeterv2Backend.Domain;

public record AgentInfo(string AgentCode, string AgentName, string Branch, bool IsValid);

public static class AgentValidator
{
    private static readonly Dictionary<string, (string Name, string Branch)> AccreditedAgents =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["AGNT-7821-MNL"] = ("Jessica Bautista", "Manila Central Branch"),
            ["AGNT-5501-CEB"] = ("Marco Tan", "Cebu Regional Branch"),
            ["AGNT-9902-DVO"] = ("Sarah Lim", "Davao Southern Branch")
        };

    public static AgentInfo Validate(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return new AgentInfo(string.Empty, string.Empty, string.Empty, false);
        }

        var trimmed = code.Trim();
        if (AccreditedAgents.TryGetValue(trimmed, out var info))
        {
            return new AgentInfo(trimmed, info.Name, info.Branch, true);
        }

        return new AgentInfo(trimmed, string.Empty, string.Empty, false);
    }
}