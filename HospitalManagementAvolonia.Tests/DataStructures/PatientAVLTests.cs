using FluentAssertions;
using HospitalManagementAvolonia.DataStructures;
using HospitalManagementAvolonia.Models;

namespace HospitalManagementAvolonia.Tests.DataStructures;

public class PatientAVLTests
{
    private readonly PatientAVL _avl = new();

    private static Patient P(string first, string last, int id = 0) =>
        TestHelpers.CreatePatient(id, first, last);

    // ============ INSERT ============

    [Fact]
    public void Insert_SingleElement_ShouldBeSearchable()
    {
        _avl.Insert(P("Ali", "Yılmaz"));

        _avl.Search("Ali", "Yılmaz").Should().NotBeNull();
        _avl.TotalNodes.Should().Be(1);
    }

    [Fact]
    public void Insert_Duplicate_ShouldNotAddTwice()
    {
        _avl.Insert(P("Ali", "Yılmaz"));
        _avl.Insert(P("Ali", "Yılmaz", 2));

        _avl.TotalNodes.Should().Be(1);
    }

    [Fact]
    public void Insert_LL_Rotation_ShouldKeepBalanced()
    {
        // Descending order triggers LL (Right Rotate)
        _avl.Insert(P("Charlie", "C", 1));
        _avl.Insert(P("Bob", "B", 2));
        _avl.Insert(P("Alice", "A", 3));

        _avl.IsBalanced().Should().BeTrue();
        _avl.TotalNodes.Should().Be(3);
    }

    [Fact]
    public void Insert_RR_Rotation_ShouldKeepBalanced()
    {
        // Ascending order triggers RR (Left Rotate)
        _avl.Insert(P("Alice", "A", 1));
        _avl.Insert(P("Bob", "B", 2));
        _avl.Insert(P("Charlie", "C", 3));

        _avl.IsBalanced().Should().BeTrue();
        _avl.TotalNodes.Should().Be(3);
    }

    [Fact]
    public void Insert_LR_Rotation_ShouldKeepBalanced()
    {
        // LR case: C, A, B
        _avl.Insert(P("Charlie", "C", 1));
        _avl.Insert(P("Alice", "A", 2));
        _avl.Insert(P("Bob", "B", 3));

        _avl.IsBalanced().Should().BeTrue();
        _avl.TotalNodes.Should().Be(3);
    }

    [Fact]
    public void Insert_RL_Rotation_ShouldKeepBalanced()
    {
        // RL case: A, C, B
        _avl.Insert(P("Alice", "A", 1));
        _avl.Insert(P("Charlie", "C", 2));
        _avl.Insert(P("Bob", "B", 3));

        _avl.IsBalanced().Should().BeTrue();
        _avl.TotalNodes.Should().Be(3);
    }

    [Fact]
    public void Insert_ManyElements_ShouldAlwaysRemainBalanced()
    {
        string[] names = { "Zeynep", "Ali", "Mehmet", "Ayşe", "Can", "Burak", "Deniz", "Elif", "Fatma", "Gül" };
        for (int i = 0; i < names.Length; i++)
        {
            _avl.Insert(P(names[i], "Test", i + 1));
            _avl.IsBalanced().Should().BeTrue($"after inserting {names[i]}");
        }

        _avl.TotalNodes.Should().Be(names.Length);
    }

    // ============ SEARCH ============

    [Fact]
    public void Search_Existing_ShouldReturnPatient()
    {
        var patient = P("Ali", "Yılmaz", 1);
        _avl.Insert(patient);

        var found = _avl.Search("Ali", "Yılmaz");

        found.Should().NotBeNull();
        found!.FirstName.Should().Be("Ali");
    }

    [Fact]
    public void Search_NonExisting_ShouldReturnNull()
    {
        _avl.Insert(P("Ali", "Yılmaz"));

        _avl.Search("Veli", "Demir").Should().BeNull();
    }

    // ============ DELETE ============

    [Fact]
    public void Delete_LeafNode_ShouldRemoveSuccessfully()
    {
        _avl.Insert(P("Bob", "B", 1));
        _avl.Insert(P("Alice", "A", 2));
        _avl.Insert(P("Charlie", "C", 3));

        _avl.Delete("Alice", "A").Should().BeTrue();
        _avl.TotalNodes.Should().Be(2);
        _avl.Search("Alice", "A").Should().BeNull();
    }

    [Fact]
    public void Delete_SingleChildNode_ShouldRemoveSuccessfully()
    {
        _avl.Insert(P("Bob", "B", 1));
        _avl.Insert(P("Alice", "A", 2));
        _avl.Insert(P("Charlie", "C", 3));
        _avl.Insert(P("Dave", "D", 4));

        _avl.Delete("Charlie", "C").Should().BeTrue();
        _avl.Search("Charlie", "C").Should().BeNull();
        _avl.Search("Dave", "D").Should().NotBeNull();
    }

    [Fact]
    public void Delete_TwoChildNode_ShouldRemoveUsingSuccessor()
    {
        _avl.Insert(P("Bob", "B", 1));
        _avl.Insert(P("Alice", "A", 2));
        _avl.Insert(P("Charlie", "C", 3));

        _avl.Delete("Bob", "B").Should().BeTrue();
        _avl.TotalNodes.Should().Be(2);
        _avl.Search("Alice", "A").Should().NotBeNull();
        _avl.Search("Charlie", "C").Should().NotBeNull();
    }

    [Fact]
    public void Delete_ShouldKeepTreeBalanced()
    {
        string[] names = { "Mehmet", "Ali", "Zeynep", "Ayşe", "Can", "Burak" };
        for (int i = 0; i < names.Length; i++)
            _avl.Insert(P(names[i], "X", i + 1));

        _avl.Delete("Mehmet", "X");
        _avl.Delete("Ali", "X");

        _avl.IsBalanced().Should().BeTrue();
    }

    [Fact]
    public void Delete_NonExisting_ShouldReturnFalse()
    {
        _avl.Insert(P("Ali", "Yılmaz"));
        _avl.Delete("Veli", "Demir").Should().BeFalse();
    }

    // ============ IN-ORDER ============

    [Fact]
    public void GetAllInOrder_ShouldReturnAlphabeticallySorted()
    {
        _avl.Insert(P("Zeynep", "Z", 1));
        _avl.Insert(P("Ali", "A", 2));
        _avl.Insert(P("Mehmet", "M", 3));

        var list = _avl.GetAllInOrder();
        var names = list.Select(p => p.FirstName + " " + p.LastName).ToList();

        names.Should().BeInAscendingOrder(StringComparer.OrdinalIgnoreCase);
    }

    // ============ STATISTICS ============

    [Fact]
    public void GetStats_ShouldReturnCorrectValues()
    {
        _avl.Insert(P("A", "X", 1));
        _avl.Insert(P("B", "X", 2));
        _avl.Insert(P("C", "X", 3));

        var (totalNodes, treeHeight, isBalanced) = _avl.GetStats();

        totalNodes.Should().Be(3);
        treeHeight.Should().BeGreaterThanOrEqualTo(2);
        isBalanced.Should().BeTrue();
    }
}
