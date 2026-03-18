using FluentAssertions;
using HospitalManagementAvolonia.DataStructures;

namespace HospitalManagementAvolonia.Tests.DataStructures;

public class AppointmentSegmentTreeTests
{
    private readonly DateTime _baseDate = new(2026, 1, 1);

    private AppointmentSegmentTree CreateTree(int capacity = 100)
        => new(_baseDate, capacity);

    // ============ BASIC ADD + QUERY ============

    [Fact]
    public void AddAppointment_SingleDay_ShouldBeQueryable()
    {
        var tree = CreateTree();
        tree.AddAppointment(_baseDate);

        tree.QueryRange(_baseDate, _baseDate).Should().Be(1);
    }

    [Fact]
    public void AddAppointment_MultipleSameDay_ShouldAccumulate()
    {
        var tree = CreateTree();
        tree.AddAppointment(_baseDate);
        tree.AddAppointment(_baseDate);
        tree.AddAppointment(_baseDate);

        tree.QueryRange(_baseDate, _baseDate).Should().Be(3);
    }

    [Fact]
    public void AddAppointment_DifferentDays_ShouldTrackSeparately()
    {
        var tree = CreateTree();
        tree.AddAppointment(_baseDate);
        tree.AddAppointment(_baseDate.AddDays(1));

        tree.QueryRange(_baseDate, _baseDate).Should().Be(1);
        tree.QueryRange(_baseDate.AddDays(1), _baseDate.AddDays(1)).Should().Be(1);
    }

    // ============ REMOVE ============

    [Fact]
    public void RemoveAppointment_ShouldDecrement()
    {
        var tree = CreateTree();
        tree.AddAppointment(_baseDate);
        tree.AddAppointment(_baseDate);

        tree.RemoveAppointment(_baseDate);

        tree.QueryRange(_baseDate, _baseDate).Should().Be(1);
    }

    [Fact]
    public void RemoveAppointment_ShouldNotGoNegative()
    {
        var tree = CreateTree();
        tree.AddAppointment(_baseDate);
        tree.RemoveAppointment(_baseDate);
        tree.RemoveAppointment(_baseDate); // Extra removal

        tree.QueryRange(_baseDate, _baseDate).Should().Be(0);
    }

    // ============ RANGE QUERIES ============

    [Fact]
    public void QueryRange_MultiDayRange_ShouldSumAll()
    {
        var tree = CreateTree();
        tree.AddAppointment(_baseDate);
        tree.AddAppointment(_baseDate.AddDays(2));
        tree.AddAppointment(_baseDate.AddDays(4));

        tree.QueryRange(_baseDate, _baseDate.AddDays(4)).Should().Be(3);
    }

    [Fact]
    public void QueryRange_EmptyRange_ShouldReturnZero()
    {
        var tree = CreateTree();
        tree.AddAppointment(_baseDate);

        tree.QueryRange(_baseDate.AddDays(10), _baseDate.AddDays(15)).Should().Be(0);
    }

    [Fact]
    public void QueryRange_PartialOverlap_ShouldCountOnlyInRange()
    {
        var tree = CreateTree();
        tree.AddAppointment(_baseDate);
        tree.AddAppointment(_baseDate.AddDays(5));
        tree.AddAppointment(_baseDate.AddDays(10));

        tree.QueryRange(_baseDate.AddDays(3), _baseDate.AddDays(7)).Should().Be(1);
    }

    // ============ BOUNDARY CONDITIONS ============

    [Fact]
    public void AddAppointment_BeforeBaseDate_ShouldBeIgnored()
    {
        var tree = CreateTree();
        tree.AddAppointment(_baseDate.AddDays(-1));

        tree.QueryRange(_baseDate.AddDays(-1), _baseDate).Should().Be(0);
    }

    [Fact]
    public void AddAppointment_BeyondCapacity_ShouldBeIgnored()
    {
        var tree = CreateTree(10); // Only 10 days
        tree.AddAppointment(_baseDate.AddDays(15)); // Out of range

        tree.QueryRange(_baseDate, _baseDate.AddDays(9)).Should().Be(0);
    }

    [Fact]
    public void QueryRange_BothOutOfBounds_ShouldReturnZero()
    {
        var tree = CreateTree(10);
        tree.QueryRange(_baseDate.AddDays(-5), _baseDate.AddDays(-1)).Should().Be(0);
    }
}
