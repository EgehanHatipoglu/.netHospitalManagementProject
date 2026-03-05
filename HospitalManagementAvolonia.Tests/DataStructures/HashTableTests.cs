using FluentAssertions;
using HospitalManagementAvolonia.DataStructures;

namespace HospitalManagementAvolonia.Tests.DataStructures;

public class HashTableTests
{
    // ============ PUT + GET ============

    [Fact]
    public void Put_Get_ShouldReturnSameValue()
    {
        var ht = new HashTable<string, int>();
        ht.Put("age", 30);

        ht.Get("age").Should().Be(30);
    }

    [Fact]
    public void Put_SameKey_ShouldUpdateValue()
    {
        var ht = new HashTable<string, string>();
        ht.Put("key", "old");
        ht.Put("key", "new");

        ht.Get("key").Should().Be("new");
        ht.Size.Should().Be(1);
    }

    [Fact]
    public void Get_NonExistingKey_ShouldReturnDefault()
    {
        var ht = new HashTable<string, string>();
        ht.Get("missing").Should().BeNull();
    }

    [Fact]
    public void Get_IntDefault_ShouldReturnZero()
    {
        var ht = new HashTable<string, int>();
        ht.Get("missing").Should().Be(0);
    }

    // ============ REMOVE ============

    [Fact]
    public void Remove_ExistingKey_ShouldReturnValueAndRemove()
    {
        var ht = new HashTable<string, int>();
        ht.Put("x", 42);

        ht.Remove("x").Should().Be(42);
        ht.Get("x").Should().Be(0);
        ht.Size.Should().Be(0);
    }

    [Fact]
    public void Remove_NonExistingKey_ShouldReturnDefault()
    {
        var ht = new HashTable<string, int>();
        ht.Remove("missing").Should().Be(0);
    }

    // ============ COLLISION ============

    [Fact]
    public void Collision_SmallCapacity_BothKeysAccessible()
    {
        // Use capacity 1 to force all keys into same bucket
        var ht = new HashTable<int, string>(1);
        ht.Put(1, "one");
        ht.Put(2, "two");
        ht.Put(3, "three");

        ht.Get(1).Should().Be("one");
        ht.Get(2).Should().Be("two");
        ht.Get(3).Should().Be("three");
        ht.Size.Should().Be(3);
    }

    // ============ CONTAINS KEY ============

    [Fact]
    public void ContainsKey_Existing_ShouldReturnTrue()
    {
        var ht = new HashTable<string, int>();
        ht.Put("key", 1);

        ht.ContainsKey("key").Should().BeTrue();
    }

    [Fact]
    public void ContainsKey_NonExisting_ShouldReturnFalse_ForReferenceTypes()
    {
        // Note: ContainsKey uses Get(key) != null, which only works for reference types.
        // For value types like int, default(int)=0 != null → always returns true (known bug).
        var ht = new HashTable<string, string>();
        ht.ContainsKey("nope").Should().BeFalse();
    }

    // ============ VALUES ============

    [Fact]
    public void Values_ShouldReturnAllStoredValues()
    {
        var ht = new HashTable<string, int>();
        ht.Put("a", 1);
        ht.Put("b", 2);
        ht.Put("c", 3);

        var values = ht.Values();
        values.Should().HaveCount(3);
        values.Should().Contain(new[] { 1, 2, 3 });
    }

    // ============ EMPTY STATE ============

    [Fact]
    public void IsEmpty_NewTable_ShouldBeTrue()
    {
        var ht = new HashTable<string, int>();
        ht.IsEmpty.Should().BeTrue();
        ht.Size.Should().Be(0);
    }

    [Fact]
    public void IsEmpty_AfterInsert_ShouldBeFalse()
    {
        var ht = new HashTable<string, int>();
        ht.Put("key", 1);
        ht.IsEmpty.Should().BeFalse();
    }
}
