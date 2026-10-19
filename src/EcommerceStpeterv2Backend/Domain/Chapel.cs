namespace EcommerceStpeterv2Backend.Domain;

public record Chapel(
    string Id,
    string Name,
    string Region,
    string Address,
    string ContactNumber,
    int TotalViewingRooms,
    bool HasCrematorium,
    bool HasAircon
);

public static class ChapelDirectory
{
    public static readonly IReadOnlyList<Chapel> AllChapels = new List<Chapel>
    {
        new("quezon-city", "St. Peter Chapel Quezon City", "NCR", "Quezon Avenue, Quezon City", "+63 2 8371-1234", 12, true, true),
        new("commonwealth", "St. Peter Chapel Commonwealth", "NCR", "Commonwealth Avenue, Quezon City", "+63 2 8931-5678", 8, true, true),
        new("cebu-mandaue", "St. Peter Chapel Mandaue", "Region VII", "A.C. Cortes Ave, Mandaue City, Cebu", "+63 32 344-9000", 10, true, true),
        new("davao-central", "St. Peter Chapel Davao", "Region XI", "McArthur Highway, Matina, Davao City", "+63 82 297-1111", 6, false, true),
        new("pampanga-angeles", "St. Peter Chapel Angeles", "Region III", "MacArthur Hwy, Angeles, Pampanga", "+63 45 625-2222", 7, true, true)
    };
}