using FluentAssertions;
using HospitalManagementAvolonia.DataStructures;
using HospitalManagementAvolonia.Models;

namespace HospitalManagementAvolonia.Tests.DataStructures;

public class PatientBSTTests
{
    private readonly PatientBST _bst = new();

    private static Patient P(string first, string last, int id = 0) =>
        TestHelpers.CreatePatient(id, first, last);

    // ============ INSERT + SEARCH ============

    [Fact]
    public void Insert_SingleElement_ShouldBeSearchable()
    {
        _bst.Insert(P("Ali", "Yılmaz"));

        _bst.Search("Ali", "Yılmaz").Should().NotBeNull();
        _bst.TotalNodes.Should().Be(1);
    }

    [Fact]
    public void Insert_Duplicate_ShouldNotAddTwice()
    {
        _bst.Insert(P("Ali", "Yılmaz"));
        _bst.Insert(P("Ali", "Yılmaz", 2));

        _bst.TotalNodes.Should().Be(1);
    }

    [Fact]
    public void Insert_MultipleElements_ShouldAllBeSearchable()
    {
        _bst.Insert(P("Bob", "B", 1));
        _bst.Insert(P("Alice", "A", 2));
        _bst.Insert(P("Charlie", "C", 3));

        _bst.Search("Bob", "B").Should().NotBeNull();
        _bst.Search("Alice", "A").Should().NotBeNull();
        _bst.Search("Charlie", "C").Should().NotBeNull();
        _bst.TotalNodes.Should().Be(3);
    }

    [Fact]
    public void Search_NonExisting_ShouldReturnNull()
    {
        _bst.Insert(P("Ali", "Yılmaz"));
        _bst.Search("Veli", "Demir").Should().BeNull();
    }

    // ============ DELETE ============

    [Fact]
    public void Delete_LeafNode_ShouldRemove()
    {
        _bst.Insert(P("Bob", "B", 1));
        _bst.Insert(P("Alice", "A", 2));
        _bst.Insert(P("Charlie", "C", 3));

        _bst.Delete("Alice", "A").Should().BeTrue();
        _bst.TotalNodes.Should().Be(2);
        _bst.Search("Alice", "A").Should().BeNull();
    }

    [Fact]
    public void Delete_SingleChildNode_ShouldRemove()
    {
        _bst.Insert(P("Bob", "B", 1));
        _bst.Insert(P("Alice", "A", 2));
        _bst.Insert(P("Charlie", "C", 3));
        _bst.Insert(P("Dave", "D", 4));

        _bst.Delete("Charlie", "C").Should().BeTrue();
        _bst.Search("Charlie", "C").Should().BeNull();
        _bst.Search("Dave", "D").Should().NotBeNull();
    }

    [Fact]
    public void Delete_TwoChildNode_ShouldRemoveUsingSuccessor()
    {
        _bst.Insert(P("Bob", "B", 1));
        _bst.Insert(P("Alice", "A", 2));
        _bst.Insert(P("Charlie", "C", 3));

        _bst.Delete("Bob", "B").Should().BeTrue();
        _bst.TotalNodes.Should().Be(2);
        _bst.Search("Alice", "A").Should().NotBeNull();
        _bst.Search("Charlie", "C").Should().NotBeNull();
    }

    [Fact]
    public void Delete_NonExisting_ShouldReturnFalse()
    {
        _bst.Insert(P("Ali", "Yılmaz"));
        _bst.Delete("Veli", "Demir").Should().BeFalse();
    }

    // ============ IN-ORDER ============

    [Fact]
    public void GetAllInOrder_ShouldReturnAlphabeticallySorted()
    {
        _bst.Insert(P("Zeynep", "Z", 1));
        _bst.Insert(P("Ali", "A", 2));
        _bst.Insert(P("Mehmet", "M", 3));

        var list = _bst.GetAllInOrder();
        var names = list.Select(p => p.FirstName + " " + p.LastName).ToList();

        names.Should().BeInAscendingOrder(StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetAllInOrder_EmptyTree_ShouldReturnEmptyList()
    {
        _bst.GetAllInOrder().Should().BeEmpty();
    }
}
