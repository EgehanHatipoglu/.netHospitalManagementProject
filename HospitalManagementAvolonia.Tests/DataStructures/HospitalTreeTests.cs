using FluentAssertions;
using HospitalManagementAvolonia.DataStructures;
using HospitalManagementAvolonia.Models;

namespace HospitalManagementAvolonia.Tests.DataStructures;

public class HospitalTreeTests
{
    private readonly HospitalTree _tree = new("Test Hastanesi");

    // ============ ADD DEPARTMENT ============

    [Fact]
    public void AddDepartmentToRoot_ShouldAppearInHierarchy()
    {
        var dept = TestHelpers.CreateDepartment(1, "Kardiyoloji");
        _tree.AddDepartmentToRoot(dept);

        var hierarchy = _tree.GetHierarchy();
        hierarchy.Should().Contain(h => h.name == "Kardiyoloji");
    }

    [Fact]
    public void AddDepartmentToRoot_Null_ShouldNotThrow()
    {
        var act = () => _tree.AddDepartmentToRoot(null!);
        act.Should().NotThrow();
    }

    [Fact]
    public void AddDepartmentToRoot_Multiple_ShouldAllAppear()
    {
        _tree.AddDepartmentToRoot(TestHelpers.CreateDepartment(1, "Kardiyoloji"));
        _tree.AddDepartmentToRoot(TestHelpers.CreateDepartment(2, "Nöroloji"));
        _tree.AddDepartmentToRoot(TestHelpers.CreateDepartment(3, "Ortopedi"));

        var hierarchy = _tree.GetHierarchy();
        // hierarchy includes root + 3 departments
        hierarchy.Should().HaveCount(4);
    }

    // ============ HIERARCHY ============

    [Fact]
    public void GetHierarchy_EmptyTree_ShouldReturnOnlyRoot()
    {
        var hierarchy = _tree.GetHierarchy();
        hierarchy.Should().HaveCount(1);
        hierarchy[0].name.Should().Be("Test Hastanesi");
        hierarchy[0].level.Should().Be(0);
    }

    [Fact]
    public void GetHierarchy_DepartmentsAtLevel1()
    {
        _tree.AddDepartmentToRoot(TestHelpers.CreateDepartment(1, "Kardiyoloji"));

        var hierarchy = _tree.GetHierarchy();
        var dept = hierarchy.First(h => h.name == "Kardiyoloji");
        dept.level.Should().Be(1);
    }

    // ============ COUNTS ============

    [Fact]
    public void GetDepartmentCount_ShouldExcludeRoot()
    {
        _tree.AddDepartmentToRoot(TestHelpers.CreateDepartment(1, "A"));
        _tree.AddDepartmentToRoot(TestHelpers.CreateDepartment(2, "B"));

        _tree.GetDepartmentCount().Should().Be(2);
    }

    [Fact]
    public void GetDepartmentCount_EmptyTree_ShouldBeZero()
    {
        _tree.GetDepartmentCount().Should().Be(0);
    }

    [Fact]
    public void GetTotalDoctorCount_ShouldSumAllDepartments()
    {
        var dept1 = TestHelpers.CreateDepartment(1, "Kardiyoloji");
        dept1.AddDoctor(TestHelpers.CreateDoctor(1, "Ali"));
        dept1.AddDoctor(TestHelpers.CreateDoctor(2, "Veli"));

        var dept2 = TestHelpers.CreateDepartment(2, "Nöroloji");
        dept2.AddDoctor(TestHelpers.CreateDoctor(3, "Ayşe"));

        _tree.AddDepartmentToRoot(dept1);
        _tree.AddDepartmentToRoot(dept2);

        _tree.GetTotalDoctorCount().Should().Be(3);
    }

    [Fact]
    public void GetTotalDoctorCount_EmptyDepartments_ShouldBeZero()
    {
        _tree.AddDepartmentToRoot(TestHelpers.CreateDepartment(1, "Boş Bölüm"));
        _tree.GetTotalDoctorCount().Should().Be(0);
    }

    [Fact]
    public void GetHierarchy_DoctorCountInDepartment_ShouldBeAccurate()
    {
        var dept = TestHelpers.CreateDepartment(1, "Kardiyoloji");
        dept.AddDoctor(TestHelpers.CreateDoctor(1));
        dept.AddDoctor(TestHelpers.CreateDoctor(2));

        _tree.AddDepartmentToRoot(dept);

        var hierarchy = _tree.GetHierarchy();
        var deptEntry = hierarchy.First(h => h.name == "Kardiyoloji");
        deptEntry.doctorCount.Should().Be(2);
    }
}
