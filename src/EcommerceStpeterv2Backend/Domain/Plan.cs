namespace EcommerceStpeterv2Backend.Domain;

public record Plan(
    string Id,
    string Name,
    string Category,
    string CasketOrUrnType,
    decimal BasePrice,
    int ViewingDays,
    bool HasReturnOfPremium,
    bool IsActive
);

public static class MemorialCatalog
{
    public static readonly IReadOnlyList<Plan> AllPlans = new List<Plan>
    {
        new("st-george", "St. George", "Casket", "Heavy metal casket, double lid, full viewing setup", 165000m, 4, true, true),
        new("st-bernadette", "St. Bernadette", "Casket", "Semi-metal casket, single lid, elegant chapel drapery", 110000m, 4, true, true),
        new("st-gregory", "St. Gregory", "Casket", "Polished hardwood casket, sealed finish, full viewing", 75000m, 4, true, true),
        new("st-claire", "St. Claire", "Casket", "Fine wood casket, glass viewing cover, affordable dignity", 52000m, 3, false, true),
        new("st-jude", "St. Jude", "Cremation", "Cremation with 3-day chapel viewing, cremation casket, marble urn", 98000m, 3, true, true),
        new("st-james", "St. James", "Cremation", "Direct cremation service, standard processing, marble urn", 62000m, 0, false, true)
    };
}
