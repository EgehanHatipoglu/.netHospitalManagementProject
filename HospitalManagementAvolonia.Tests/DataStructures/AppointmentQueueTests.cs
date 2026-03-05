using FluentAssertions;
using HospitalManagementAvolonia.DataStructures;
using HospitalManagementAvolonia.Models;

namespace HospitalManagementAvolonia.Tests.DataStructures;

public class AppointmentQueueTests
{
    private readonly AppointmentQueue _queue = new();

    private Appointment MakeAppointment(int id) => TestHelpers.CreateAppointment(id);

    // ============ FIFO ============

    [Fact]
    public void EnqueueDequeue_ShouldPreserveFIFOOrder()
    {
        var a1 = MakeAppointment(1);
        var a2 = MakeAppointment(2);
        var a3 = MakeAppointment(3);

        _queue.Enqueue(a1);
        _queue.Enqueue(a2);
        _queue.Enqueue(a3);

        _queue.Dequeue().Should().BeSameAs(a1);
        _queue.Dequeue().Should().BeSameAs(a2);
        _queue.Dequeue().Should().BeSameAs(a3);
    }

    // ============ EMPTY DEQUEUE ============

    [Fact]
    public void Dequeue_EmptyQueue_ShouldReturnNull()
    {
        _queue.Dequeue().Should().BeNull();
    }

    // ============ SIZE ============

    [Fact]
    public void Size_ShouldReturnCorrectCount()
    {
        _queue.IsEmpty.Should().BeTrue();
        _queue.Size.Should().Be(0);

        _queue.Enqueue(MakeAppointment(1));
        _queue.Enqueue(MakeAppointment(2));

        _queue.Size.Should().Be(2);
        _queue.IsEmpty.Should().BeFalse();

        _queue.Dequeue();
        _queue.Size.Should().Be(1);
    }

    // ============ GET ALL ============

    [Fact]
    public void GetAll_ShouldReturnAllInOrder()
    {
        var a1 = MakeAppointment(1);
        var a2 = MakeAppointment(2);

        _queue.Enqueue(a1);
        _queue.Enqueue(a2);

        var all = _queue.GetAll();
        all.Should().HaveCount(2);
        all[0].Should().BeSameAs(a1);
        all[1].Should().BeSameAs(a2);
    }

    [Fact]
    public void GetAll_EmptyQueue_ShouldReturnEmptyList()
    {
        _queue.GetAll().Should().BeEmpty();
    }

    // ============ ENQUEUE AFTER DRAIN ============

    [Fact]
    public void EnqueueAfterFullDrain_ShouldWorkCorrectly()
    {
        _queue.Enqueue(MakeAppointment(1));
        _queue.Dequeue();

        _queue.IsEmpty.Should().BeTrue();

        var a2 = MakeAppointment(2);
        _queue.Enqueue(a2);

        _queue.Size.Should().Be(1);
        _queue.Dequeue().Should().BeSameAs(a2);
    }
}
