namespace EcommerceStpeterv2Backend.Domain;

public record BeneficiaryInput(
    string FullName,
    string Relationship,
    DateTime? BirthDate,
    decimal PercentageShare,
    bool IsPrimary = true
);

public static class BeneficiaryValidator
{
    public static (bool IsValid, string? ErrorMessage) Validate(IEnumerable<BeneficiaryInput>? beneficiaries)
    {
        if (beneficiaries == null || !beneficiaries.Any())
        {
            return (false, "At least one primary beneficiary must be designated.");
        }

        var list = beneficiaries.ToList();
        // JSON can carry a null entry ("beneficiaries": [null]); it is invalid
        // input, not a server error.
        if (list.Exists(b => b is null))
        {
            return (false, "Each beneficiary must be an object.");
        }

        var primaryList = list.Where(b => b.IsPrimary).ToList();
        if (!primaryList.Any())
        {
            return (false, "At least one primary beneficiary is required.");
        }

        foreach (var b in primaryList)
        {
            if (string.IsNullOrWhiteSpace(b.FullName))
            {
                return (false, "Beneficiary full name is required.");
            }
            if (b.PercentageShare <= 0)
            {
                return (false, "Beneficiary percentage share must be greater than zero.");
            }
        }

        var totalPercentage = primaryList.Sum(b => b.PercentageShare);
        if (totalPercentage != 100m)
        {
            return (false, $"Total percentage is {totalPercentage}%. Total primary allocation must equal exactly 100%.");
        }

        return (true, null);
    }
}
