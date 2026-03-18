using FluentAssertions;
using HospitalManagementAvolonia.DataStructures;

namespace HospitalManagementAvolonia.Tests.DataStructures;

public class UndoStackTests
{
    private readonly UndoStack _stack = new();

    // ============ PUSH + POP (LIFO) ============

    [Fact]
    public void Push_Pop_ShouldReturnLastPushed()
    {
        _stack.Push("delete_patient");
        _stack.Push("add_appointment");

        _stack.Pop().Should().Be("add_appointment");
        _stack.Pop().Should().Be("delete_patient");
    }

    [Fact]
    public void Pop_EmptyStack_ShouldReturnNull()
    {
        _stack.Pop().Should().BeNull();
    }

    [Fact]
    public void IsEmpty_NewStack_ShouldBeTrue()
    {
        _stack.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void IsEmpty_AfterPush_ShouldBeFalse()
    {
        _stack.Push("op");
        _stack.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public void IsEmpty_AfterPushAndPop_ShouldBeTrue()
    {
        _stack.Push("op");
        _stack.Pop();
        _stack.IsEmpty.Should().BeTrue();
    }

    // ============ POP WITH DATA ============

    [Fact]
    public void PopWithData_ShouldReturnBothOperationAndData()
    {
        var data = new { Id = 1, Name = "Ali" };
        _stack.Push("delete_patient", data);

        var node = _stack.PopWithData();
        node.Should().NotBeNull();
        node!.Operation.Should().Be("delete_patient");
        node.Data.Should().Be(data);
    }

    [Fact]
    public void PopWithData_EmptyStack_ShouldReturnNull()
    {
        _stack.PopWithData().Should().BeNull();
    }

    [Fact]
    public void PopWithData_PushWithoutData_DataShouldBeNull()
    {
        _stack.Push("simple_op");
        var node = _stack.PopWithData();
        node!.Data.Should().BeNull();
    }

    // ============ PEEK ============

    [Fact]
    public void PeekOperation_ShouldNotRemoveItem()
    {
        _stack.Push("op1");
        _stack.Push("op2");

        _stack.PeekOperation().Should().Be("op2");
        _stack.PeekOperation().Should().Be("op2"); // still there
        _stack.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public void PeekOperation_EmptyStack_ShouldReturnNull()
    {
        _stack.PeekOperation().Should().BeNull();
    }

    [Fact]
    public void PeekData_ShouldReturnTopData()
    {
        _stack.Push("op", 42);
        _stack.PeekData().Should().Be(42);
    }

    [Fact]
    public void PeekData_EmptyStack_ShouldReturnNull()
    {
        _stack.PeekData().Should().BeNull();
    }

    // ============ CHAINED OPERATIONS ============

    [Fact]
    public void MultiplePush_ShouldPopInReverseOrder()
    {
        _stack.Push("first");
        _stack.Push("second");
        _stack.Push("third");

        _stack.Pop().Should().Be("third");
        _stack.Pop().Should().Be("second");
        _stack.Pop().Should().Be("first");
        _stack.Pop().Should().BeNull();
    }
}
