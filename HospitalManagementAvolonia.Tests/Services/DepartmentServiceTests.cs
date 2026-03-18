using FluentAssertions;
using HospitalManagementAvolonia.Data;
using HospitalManagementAvolonia.Models;
using HospitalManagementAvolonia.Services;
using Moq;

namespace HospitalManagementAvolonia.Tests.Services;

public class DepartmentServiceTests
{
    private readonly Mock<IDatabaseService> _mockDb;
    private readonly DepartmentService _service;

    public DepartmentServiceTests()
    {
        _mockDb = new Mock<IDatabaseService>();
        _mockDb.Setup(d => d.LoadDepartmentsAsync())
            .ReturnsAsync(new List<(int id, string name, int capacity)>());
        _mockDb.Setup(d => d.SaveDepartmentAsync(It.IsAny<Department>()))
            .Returns(Task.CompletedTask);

        _service = new DepartmentService(_mockDb.Object);
    }

    // ============ INITIALIZATION ============

    [Fact]
    public async Task InitializeAsync_ShouldLoadFromDatabase()
    {
        _mockDb.Setup(d => d.LoadDepartmentsAsync())
            .ReturnsAsync(new List<(int, string, int)>
            {
                (1, "Kardiyoloji", 20),
                (2, "Nöroloji", 15)
            });

        var service = new DepartmentService(_mockDb.Object);
        await service.InitializeAsync();

        var depts = await service.GetAllDepartmentsAsync();
        depts.Should().HaveCount(2);
    }

    [Fact]
    public async Task InitializeAsync_CalledTwice_ShouldQueryDBOnce()
    {
        var service = new DepartmentService(_mockDb.Object);
        await service.InitializeAsync();
        await service.InitializeAsync();

        _mockDb.Verify(d => d.LoadDepartmentsAsync(), Times.Once);
    }

    // ============ CRUD ============

    [Fact]
    public async Task AddDepartmentAsync_ShouldCallSave()
    {
        await _service.InitializeAsync();
        var dept = await _service.AddDepartmentAsync("Ortopedi", 10);

        dept.Name.Should().Be("Ortopedi");
        dept.Capacity.Should().Be(10);
        _mockDb.Verify(d => d.SaveDepartmentAsync(It.Is<Department>(dep => dep.Name == "Ortopedi")), Times.Once);
    }

    [Fact]
    public async Task AddDepartmentAsync_ShouldAutoIncrementId()
    {
        await _service.InitializeAsync();
        var dept1 = await _service.AddDepartmentAsync("A", 10);
        var dept2 = await _service.AddDepartmentAsync("B", 10);

        dept2.Id.Should().BeGreaterThan(dept1.Id);
    }

    [Fact]
    public async Task GetDepartmentByIdAsync_ExistingId_ShouldReturn()
    {
        await _service.InitializeAsync();
        var dept = await _service.AddDepartmentAsync("Kardiyoloji", 20);

        var result = await _service.GetDepartmentByIdAsync(dept.Id);
        result.Should().NotBeNull();
        result!.Name.Should().Be("Kardiyoloji");
    }

    [Fact]
    public async Task GetDepartmentByIdAsync_NonExistingId_ShouldReturnNull()
    {
        await _service.InitializeAsync();
        var result = await _service.GetDepartmentByIdAsync(999);
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllDepartmentsAsync_ShouldReturnAlphabetical()
    {
        await _service.InitializeAsync();
        await _service.AddDepartmentAsync("Nöroloji", 10);
        await _service.AddDepartmentAsync("Kardiyoloji", 20);
        await _service.AddDepartmentAsync("Ortopedi", 15);

        var depts = await _service.GetAllDepartmentsAsync();
        depts.Select(d => d.Name).Should().BeInAscendingOrder();
    }

    // ============ HIERARCHY ============

    [Fact]
    public async Task GetHierarchy_AfterAddDepartment_ShouldContainNewDept()
    {
        await _service.InitializeAsync();
        await _service.AddDepartmentAsync("Kardiyoloji", 20);

        var hierarchy = _service.GetHierarchy();
        hierarchy.Should().Contain(h => h.name == "Kardiyoloji");
    }

    [Fact]
    public async Task GetHierarchy_ShouldIncludeRoot()
    {
        await _service.InitializeAsync();

        var hierarchy = _service.GetHierarchy();
        hierarchy.Should().Contain(h => h.level == 0);
    }
}
