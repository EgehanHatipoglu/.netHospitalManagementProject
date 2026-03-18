using FluentAssertions;
using HospitalManagementAvolonia.DataStructures;
using HospitalManagementAvolonia.Models;

namespace HospitalManagementAvolonia.Tests.DataStructures;

public class DoctorGraphTests
{
    private readonly DoctorGraph _graph = new();

    private static Doctor D(int id, string first = "Dr", string last = "Test") =>
        TestHelpers.CreateDoctor(id, first, last);

    // ============ STRUCTURE ============

    [Fact]
    public void AddDoctor_ShouldAppearInGetAllDoctors()
    {
        _graph.AddDoctor(D(1, "Ali"));
        _graph.AddDoctor(D(2, "Veli"));

        _graph.GetAllDoctors().Should().HaveCount(2);
    }

    [Fact]
    public void AddDoctor_Null_ShouldDoNothing()
    {
        _graph.AddDoctor(null!);
        _graph.GetAllDoctors().Should().BeEmpty();
    }

    [Fact]
    public void AddDoctor_Duplicate_ShouldNotAddTwice()
    {
        _graph.AddDoctor(D(1));
        _graph.AddDoctor(D(1));

        _graph.GetAllDoctors().Should().HaveCount(1);
    }

    [Fact]
    public void AddReferral_ShouldAppearInGetReferrals()
    {
        var d1 = D(1, "Ali");
        var d2 = D(2, "Veli");

        _graph.AddReferral(d1, d2);

        _graph.GetReferrals(1).Should().Contain(d2);
    }

    [Fact]
    public void AddReferral_Duplicate_ShouldNotAddTwice()
    {
        var d1 = D(1);
        var d2 = D(2);

        _graph.AddReferral(d1, d2);
        _graph.AddReferral(d1, d2);

        _graph.GetReferrals(1).Should().HaveCount(1);
    }

    [Fact]
    public void RemoveDoctor_ShouldRemoveAllConnections()
    {
        var d1 = D(1);
        var d2 = D(2);
        var d3 = D(3);

        _graph.AddReferral(d1, d2);
        _graph.AddReferral(d3, d1); // d3 -> d1

        _graph.RemoveDoctor(1);

        _graph.GetAllDoctors().Should().HaveCount(2);
        _graph.GetReferrals(3).Should().NotContain(d => d.Id == 1);
    }

    [Fact]
    public void GetReferrals_NonExisting_ShouldReturnEmpty()
    {
        _graph.GetReferrals(999).Should().BeEmpty();
    }

    // ============ BFS ============

    [Fact]
    public void BFS_DirectConnection_ShouldFindPathDistance1()
    {
        var d1 = D(1, "Ali");
        var d2 = D(2, "Veli");

        _graph.AddReferral(d1, d2);

        var result = _graph.BFS(1, 2);
        result.Should().Contain(s => s.Contains("1 adım"));
    }

    [Fact]
    public void BFS_IndirectConnection_ShouldFindShortestPath()
    {
        var d1 = D(1, "Ali");
        var d2 = D(2, "Veli");
        var d3 = D(3, "Ayşe");

        _graph.AddReferral(d1, d2);
        _graph.AddReferral(d2, d3);

        var result = _graph.BFS(1, 3);
        result.Should().Contain(s => s.Contains("2 adım"));
    }

    [Fact]
    public void BFS_NoConnection_ShouldReturnNotFoundMessage()
    {
        var d1 = D(1, "Ali");
        var d2 = D(2, "Veli");

        _graph.AddDoctor(d1);
        _graph.AddDoctor(d2);

        var result = _graph.BFS(1, 2);
        result.Should().Contain(s => s.Contains("bulunamadı"));
    }

    [Fact]
    public void BFS_SameStartAndTarget_ShouldReturnSpecialMessage()
    {
        _graph.AddDoctor(D(1, "Ali"));

        var result = _graph.BFS(1, 1);
        result.Should().Contain(s => s.Contains("aynı"));
    }

    [Fact]
    public void BFS_InvalidIds_ShouldReturnErrorMessage()
    {
        var result = _graph.BFS(99, 100);
        result.Should().Contain(s => s.Contains("Geçersiz"));
    }

    // ============ DFS ============

    [Fact]
    public void DFS_ShouldVisitAllReachableNodes()
    {
        var d1 = D(1, "Ali", "A");
        var d2 = D(2, "Veli", "B");
        var d3 = D(3, "Ayşe", "C");

        _graph.AddReferral(d1, d2);
        _graph.AddReferral(d2, d3);

        var result = _graph.DFS(1);
        result.Should().HaveCount(3);
    }

    [Fact]
    public void DFS_WithCycle_ShouldNotInfiniteLoop()
    {
        var d1 = D(1, "Ali", "A");
        var d2 = D(2, "Veli", "B");

        _graph.AddReferral(d1, d2);
        _graph.AddReferral(d2, d1); // cycle

        var result = _graph.DFS(1);
        result.Should().HaveCount(2); // Should visit each only once
    }

    [Fact]
    public void DFS_DisconnectedNode_ShouldReturnOnlySelf()
    {
        var d1 = D(1, "Ali", "A");
        var d2 = D(2, "Veli", "B");

        _graph.AddDoctor(d1);
        _graph.AddDoctor(d2);

        var result = _graph.DFS(1);
        result.Should().HaveCount(1);
    }

    [Fact]
    public void DFS_InvalidId_ShouldReturnEmpty()
    {
        _graph.DFS(999).Should().BeEmpty();
    }
}
