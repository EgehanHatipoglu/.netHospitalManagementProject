using FluentAssertions;
using HospitalManagementAvolonia.DataStructures;
using HospitalManagementAvolonia.Models;
using static HospitalManagementAvolonia.DataStructures.ERPriorityQueue;

namespace HospitalManagementAvolonia.Tests.DataStructures;

public class ERPriorityQueueTests
{
    private readonly ERPriorityQueue _queue = new();

    private static Patient P(int id) => TestHelpers.CreatePatient(id, $"Patient{id}", "Test");

    // ============ ADD + PRIORITY ORDER ============

    [Fact]
    public void AddPatient_HighSeverityExtractedFirst()
    {
        _queue.AddPatient(new ERPatient(P(1), 1, "Headache"));
        _queue.AddPatient(new ERPatient(P(2), 10, "Cardiac Arrest"));
        _queue.AddPatient(new ERPatient(P(3), 5, "Fracture"));

        var first = _queue.RemoveHighestPriority();
        first.Should().NotBeNull();
        first!.Severity.Should().Be(10);
    }

    [Fact]
    public void AddPatient_EqualSeverity_BothAccessible()
    {
        _queue.AddPatient(new ERPatient(P(1), 7, "Burn"));
        _queue.AddPatient(new ERPatient(P(2), 7, "Cut"));

        _queue.Size.Should().Be(2);
        var first = _queue.RemoveHighestPriority();
        var second = _queue.RemoveHighestPriority();

        first!.Severity.Should().Be(7);
        second!.Severity.Should().Be(7);
    }

    // ============ REMOVE ============

    [Fact]
    public void RemoveHighestPriority_EmptyHeap_ShouldReturnNull()
    {
        _queue.RemoveHighestPriority().Should().BeNull();
    }

    [Fact]
    public void RemoveHighestPriority_HeapPropertyPreserved()
    {
        int[] severities = { 3, 7, 1, 9, 5, 2, 8 };
        for (int i = 0; i < severities.Length; i++)
            _queue.AddPatient(new ERPatient(P(i + 1), severities[i], $"Complaint{i}"));

        int lastSeverity = int.MaxValue;
        while (!_queue.IsEmpty)
        {
            var patient = _queue.RemoveHighestPriority()!;
            patient.Severity.Should().BeLessThanOrEqualTo(lastSeverity);
            lastSeverity = patient.Severity;
        }
    }

    // ============ GET ALL SORTED ============

    [Fact]
    public void GetAllSorted_ShouldReturnDescendingSeverity()
    {
        _queue.AddPatient(new ERPatient(P(1), 3, "Mild"));
        _queue.AddPatient(new ERPatient(P(2), 9, "Critical"));
        _queue.AddPatient(new ERPatient(P(3), 6, "Moderate"));

        var sorted = _queue.GetAllSorted();
        sorted.Select(p => p.Severity).Should().BeInDescendingOrder();
    }

    // ============ REMOVE BY ID ============

    [Fact]
    public void RemovePatientById_ShouldRemoveAndKeepHeapIntact()
    {
        _queue.AddPatient(new ERPatient(P(1), 3, "A"));
        _queue.AddPatient(new ERPatient(P(2), 8, "B"));
        _queue.AddPatient(new ERPatient(P(3), 5, "C"));

        _queue.RemovePatientById(2).Should().BeTrue();
        _queue.Size.Should().Be(2);

        // Highest remaining should be severity 5
        var top = _queue.RemoveHighestPriority();
        top!.Severity.Should().Be(5);
    }

    [Fact]
    public void RemovePatientById_NonExisting_ShouldReturnFalse()
    {
        _queue.AddPatient(new ERPatient(P(1), 5, "Test"));
        _queue.RemovePatientById(999).Should().BeFalse();
    }

    // ============ SEVERITY VALIDATION ============

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    [InlineData(-1)]
    public void ERPatient_InvalidSeverity_ShouldThrow(int severity)
    {
        var act = () => new ERPatient(P(1), severity, "Test");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ERPatient_NullPatient_ShouldThrow()
    {
        var act = () => new ERPatient(null!, 5, "Test");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ERPatient_EmptyComplaint_ShouldThrow()
    {
        var act = () => new ERPatient(P(1), 5, "");
        act.Should().Throw<ArgumentException>();
    }
}
