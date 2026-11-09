using RaceDay.Web.Services;

namespace RaceDay.Web.Tests;

public class FormattingTests
{
    [Theory]
    [InlineData(1, "1st")]
    [InlineData(2, "2nd")]
    [InlineData(3, "3rd")]
    [InlineData(4, "4th")]
    [InlineData(11, "11th")]
    [InlineData(12, "12th")]
    [InlineData(13, "13th")]
    [InlineData(21, "21st")]
    [InlineData(22, "22nd")]
    [InlineData(47, "47th")]
    [InlineData(101, "101st")]
    [InlineData(111, "111th")]
    public void Ordinal_FormatsPositions(int number, string expected) =>
        Assert.Equal(expected, Formatting.Ordinal(number));

    [Fact]
    public void PositionText_ShowsPositionOutOfFinishers() =>
        Assert.Equal("47th out of 312 finishers", Formatting.PositionText(47, 312));

    [Fact]
    public void PositionText_WithoutPosition_ShowsDash() =>
        Assert.Equal("—", Formatting.PositionText(null, 10));

    [Fact]
    public void Duration_FormatsAsHoursMinutesSeconds() =>
        Assert.Equal("1:05:09", Formatting.Duration(new TimeSpan(1, 5, 9)));

    [Theory]
    [InlineData("Confirmed", "status-confirmed")]
    [InlineData("Pending", "status-pending")]
    [InlineData("Cancelled", "status-cancelled")]
    [InlineData("whatever", "status-default")]
    public void StatusClass_MapsStatusToColour(string status, string expected) =>
        Assert.Equal(expected, Formatting.StatusClass(status));

    [Theory]
    [InlineData("0:48:30", true)]
    [InlineData("01:05:09", true)]
    [InlineData("12:00:00", true)]
    [InlineData("45:30", false)]       // ambiguous minutes:seconds is rejected
    [InlineData("abc", false)]
    [InlineData("", false)]
    [InlineData("0:00:00", false)]     // must be greater than zero
    [InlineData("1:65:00", false)]
    public void TryParseFinishTime_AcceptsOnlyHMmSs(string input, bool expected) =>
        Assert.Equal(expected, Formatting.TryParseFinishTime(input, out _));

    [Fact]
    public void TryParseFinishTime_ParsesValue()
    {
        Assert.True(Formatting.TryParseFinishTime("1:05:09", out var time));
        Assert.Equal(new TimeSpan(1, 5, 9), time);
    }
}
