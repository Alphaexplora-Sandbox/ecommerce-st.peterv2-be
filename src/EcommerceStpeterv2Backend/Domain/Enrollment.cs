namespace EcommerceStpeterv2Backend.Domain;

public record Enrollment(
    string Id,
    string UserId,
    string PlanId,
    string ContractNumber,
    string AssigneeType,
    string AssigneeName,
    DateTime AssigneeDob,
    string AssigneeRelationship,
    string PaymentTerm,
    decimal TotalContractPrice,
    decimal DiscountAmount,
    decimal NetContractPrice,
    decimal InstallmentAmount,
    int TotalInstallments,
    int InstallmentsPaid,
    string Status,
    bool HasInsuranceRider,
    DateTime NextDueDate,
    DateTime CreatedAt
);

public static class EnrollmentPolicy
{
    public static (bool HasInsuranceRider, string PolicyNotice) EvaluateAgeEligibility(DateTime dob, DateTime? asOf = null)
    {
        var referenceDate = asOf ?? DateTime.UtcNow;
        int age = referenceDate.Year - dob.Year;
        if (dob.Date > referenceDate.AddYears(-age)) age--;

        if (age >= 18 && age <= 60)
        {
            return (true, "Eligible for full life insurance and accidental death rider.");
        }
        if (age > 60)
        {
            return (false, "Guaranteed Memorial Service active. Insurance rider waived for planholders above age 60.");
        }
        return (false, "Planholder must be at least 18 years of age or enrolled under legal guardian.");
    }

    public static string GenerateContractNumber()
    {
        var random = Random.Shared.Next(10000, 99999);
        return $"STP-2026-{random}";
    }
}
