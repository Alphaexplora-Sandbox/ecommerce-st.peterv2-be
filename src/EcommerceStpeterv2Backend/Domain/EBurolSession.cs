namespace EcommerceStpeterv2Backend.Domain;

public record TributeMessage(
    string Id,
    string SessionId,
    string SenderName,
    string MessageBody,
    string RelationshipToDeceased,
    DateTime CreatedAt
);

public record EBurolSession(
    string Id,
    string ChapelId,
    string DeceasedName,
    string FamilyContactName,
    string AccessPin,
    string StreamUrl,
    DateTime ScheduledStart,
    DateTime ScheduledEnd,
    int CandleCount,
    bool IsActive
);

public static class EBurolStore
{
    private static readonly object LockObj = new();
    private static readonly Dictionary<string, EBurolSession> Sessions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["EBR-402"] = new(
            "EBR-402",
            "quezon-city",
            "Ramon 'Tatay Mon' Cruz",
            "Rosa Cruz",
            "882194",
            "https://stream.stpeter.com.ph/live/EBR-402/index.m3u8",
            DateTime.UtcNow.AddHours(-2),
            DateTime.UtcNow.AddDays(3),
            142,
            true
        )
    };

    private static readonly List<TributeMessage> Tributes = new()
    {
        new(Guid.NewGuid().ToString(), "EBR-402", "Maria Santos", "Sending our prayers from Dubai. We love you Tito Ramon.", "Niece", DateTime.UtcNow.AddHours(-1))
    };

    public static EBurolSession? GetSession(string id)
    {
        lock (LockObj)
        {
            return Sessions.TryGetValue(id, out var session) ? session : null;
        }
    }

    public static (bool Success, string? ErrorMessage, EBurolSession? Session) AccessSession(string id, string? pin)
    {
        lock (LockObj)
        {
            if (!Sessions.TryGetValue(id, out var session))
            {
                return (false, "E-Burol session not found.", null);
            }

            if (string.IsNullOrWhiteSpace(pin) || !string.Equals(session.AccessPin, pin.Trim()))
            {
                return (false, "PIN Required to respect family privacy.", null);
            }

            return (true, null, session);
        }
    }

    public static int IncrementCandle(string id)
    {
        lock (LockObj)
        {
            if (Sessions.TryGetValue(id, out var session))
            {
                var updated = session with { CandleCount = session.CandleCount + 1 };
                Sessions[id] = updated;
                return updated.CandleCount;
            }
            return 0;
        }
    }

    public static TributeMessage AddTribute(string sessionId, string senderName, string message, string relationship)
    {
        lock (LockObj)
        {
            var msg = new TributeMessage(Guid.NewGuid().ToString(), sessionId, senderName, message, relationship, DateTime.UtcNow);
            Tributes.Add(msg);
            return msg;
        }
    }

    public static IReadOnlyList<TributeMessage> GetTributes(string sessionId)
    {
        lock (LockObj)
        {
            return Tributes.Where(t => string.Equals(t.SessionId, sessionId, StringComparison.OrdinalIgnoreCase)).ToList();
        }
    }
}
