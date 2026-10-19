using EcommerceStpeterv2Backend.Domain;
using Xunit;

namespace EcommerceStpeterv2Backend.Tests;

public class EBurolStoreTests
{
    [Fact]
    public void Access_With_Valid_Pin_Succeeds()
    {
        var (success, error, session) = EBurolStore.AccessSession("EBR-402", "882194");

        Assert.True(success);
        Assert.Null(error);
        Assert.NotNull(session);
        Assert.Contains("m3u8", session!.StreamUrl);
    }

    [Fact]
    public void Access_With_Wrong_Pin_Fails()
    {
        var (success, error, session) = EBurolStore.AccessSession("EBR-402", "000000");

        Assert.False(success);
        Assert.Contains("PIN Required", error);
        Assert.Null(session);
    }

    [Fact]
    public void Increment_Candle_Increases_Count()
    {
        var initial = EBurolStore.GetSession("EBR-402")?.CandleCount ?? 0;
        var updated = EBurolStore.IncrementCandle("EBR-402");

        Assert.Equal(initial + 1, updated);
    }

    [Fact]
    public void Add_And_Get_Tributes_Work()
    {
        var tribute = EBurolStore.AddTribute("EBR-402", "Elena", "Rest in peace", "Friend");

        Assert.NotNull(tribute);
        Assert.Equal("Elena", tribute.SenderName);

        var tributes = EBurolStore.GetTributes("EBR-402");
        Assert.Contains(tributes, t => t.SenderName == "Elena");
    }
}