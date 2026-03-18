using FluentAssertions;
using HospitalManagementAvolonia.DataStructures;
using HospitalManagementAvolonia.Models;

namespace HospitalManagementAvolonia.Tests.DataStructures;

public class PatientLRUCacheTests
{
    private static Patient P(int id, string first = "Patient", string last = "Test") =>
        TestHelpers.CreatePatient(id, first, last);

    // ============ CAPACITY ============

    [Fact]
    public void AccessPatient_ExceedCapacity_ShouldEvictLRU()
    {
        var cache = new PatientLRUCache(3);
        cache.AccessPatient(P(1, "Ali"));
        cache.AccessPatient(P(2, "Veli"));
        cache.AccessPatient(P(3, "Ayşe"));
        cache.AccessPatient(P(4, "Fatma")); // should evict Ali (id=1)

        cache.Count.Should().Be(3);
        var recent = cache.GetRecentPatients();
        recent.Select(p => p.Id).Should().NotContain(1);
        recent.Select(p => p.Id).Should().Contain(4);
    }

    [Fact]
    public void GetRecentPatients_ShouldReturnMRUFirst()
    {
        var cache = new PatientLRUCache(5);
        cache.AccessPatient(P(1));
        cache.AccessPatient(P(2));
        cache.AccessPatient(P(3));

        var recent = cache.GetRecentPatients();
        recent.Select(p => p.Id).Should().Equal(3, 2, 1);
    }

    // ============ ACCESS REORDER ============

    [Fact]
    public void AccessPatient_ExistingPatient_ShouldMoveToFront()
    {
        var cache = new PatientLRUCache(5);
        cache.AccessPatient(P(1));
        cache.AccessPatient(P(2));
        cache.AccessPatient(P(3));
        cache.AccessPatient(P(1)); // Re-access, should move to front

        var recent = cache.GetRecentPatients();
        recent.First().Id.Should().Be(1);
    }

    [Fact]
    public void AccessPatient_ReAccessPreventsEviction()
    {
        var cache = new PatientLRUCache(3);
        cache.AccessPatient(P(1));
        cache.AccessPatient(P(2));
        cache.AccessPatient(P(3));
        cache.AccessPatient(P(1)); // Pull to front → P(2) is now LRU
        cache.AccessPatient(P(4)); // Should evict P(2) not P(1)

        cache.Count.Should().Be(3);
        var ids = cache.GetRecentPatients().Select(p => p.Id).ToList();
        ids.Should().Contain(1);
        ids.Should().NotContain(2);
    }

    // ============ EDGE CASES ============

    [Fact]
    public void AccessPatient_NullPatient_ShouldDoNothing()
    {
        var cache = new PatientLRUCache(3);
        cache.AccessPatient(null!);
        cache.Count.Should().Be(0);
    }

    [Fact]
    public void AccessPatient_SamePatientTwice_ShouldNotDuplicate()
    {
        var cache = new PatientLRUCache(5);
        cache.AccessPatient(P(1));
        cache.AccessPatient(P(1));

        cache.Count.Should().Be(1);
        cache.GetRecentPatients().Should().HaveCount(1);
    }

    [Fact]
    public void RemovePatient_ShouldRemoveFromCache()
    {
        var cache = new PatientLRUCache(5);
        cache.AccessPatient(P(1));
        cache.AccessPatient(P(2));

        cache.RemovePatient(1);

        cache.Count.Should().Be(1);
        cache.GetRecentPatients().Select(p => p.Id).Should().NotContain(1);
    }

    [Fact]
    public void RemovePatient_NonExistingId_ShouldNotThrow()
    {
        var cache = new PatientLRUCache(5);
        cache.AccessPatient(P(1));

        var act = () => cache.RemovePatient(999);
        act.Should().NotThrow();
        cache.Count.Should().Be(1);
    }

    [Fact]
    public void GetRecentPatients_EmptyCache_ShouldReturnEmptyList()
    {
        var cache = new PatientLRUCache(5);
        cache.GetRecentPatients().Should().BeEmpty();
    }
}
