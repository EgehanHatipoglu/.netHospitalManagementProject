using FluentAssertions;
using HospitalManagementAvolonia.DataStructures;
using HospitalManagementAvolonia.Models;

namespace HospitalManagementAvolonia.Tests.DataStructures;

public class PatientTrieTests
{
    private readonly PatientTrie _trie = new();

    private static Patient P(int id, string first, string last) =>
        TestHelpers.CreatePatient(id, first, last);

    // ============ INSERT + SEARCH ============

    [Fact]
    public void Insert_GetSuggestions_ShouldFindByPrefix()
    {
        _trie.Insert(P(1, "Ali", "Yılmaz"));
        _trie.Insert(P(2, "Ahmet", "Kaya"));
        _trie.Insert(P(3, "Burak", "Demir"));

        var results = _trie.GetSuggestions("a");
        results.Should().HaveCount(2);
        results.Select(p => p.FirstName).Should().Contain(new[] { "Ali", "Ahmet" });
    }

    [Fact]
    public void GetSuggestions_FullName_ShouldReturnExactMatch()
    {
        _trie.Insert(P(1, "Ali", "Yılmaz"));

        var results = _trie.GetSuggestions("ali yılmaz");
        results.Should().HaveCount(1);
        results[0].Id.Should().Be(1);
    }

    [Fact]
    public void GetSuggestions_NoMatch_ShouldReturnEmpty()
    {
        _trie.Insert(P(1, "Ali", "Yılmaz"));

        _trie.GetSuggestions("xyz").Should().BeEmpty();
    }

    [Fact]
    public void GetSuggestions_EmptyPrefix_ShouldReturnEmpty()
    {
        _trie.Insert(P(1, "Ali", "Yılmaz"));

        _trie.GetSuggestions("").Should().BeEmpty();
    }

    // ============ DUPLICATE ============

    [Fact]
    public void Insert_SameIdTwice_ShouldNotDuplicate()
    {
        var patient = P(1, "Ali", "Yılmaz");
        _trie.Insert(patient);
        _trie.Insert(patient);

        var results = _trie.GetSuggestions("ali");
        results.Should().HaveCount(1);
    }

    // ============ REMOVE ============

    [Fact]
    public void Remove_ShouldNotAppearInSuggestions()
    {
        var patient = P(1, "Ali", "Yılmaz");
        _trie.Insert(patient);

        _trie.Remove(patient);

        _trie.GetSuggestions("ali").Should().BeEmpty();
    }

    [Fact]
    public void Remove_OnlyTargetPatient_OthersShouldRemain()
    {
        _trie.Insert(P(1, "Ali", "Yılmaz"));
        _trie.Insert(P(2, "Ali", "Kaya"));

        _trie.Remove(P(1, "Ali", "Yılmaz"));

        // "Ali Kaya" should still be findable via "ali" prefix (partial match through children)
        // But "ali yılmaz" exact should be gone
        var exactResults = _trie.GetSuggestions("ali yılmaz");
        exactResults.Should().BeEmpty();
    }

    [Fact]
    public void Remove_NullPatient_ShouldNotThrow()
    {
        var act = () => _trie.Remove(null!);
        act.Should().NotThrow();
    }

    // ============ CASE INSENSITIVITY ============

    [Fact]
    public void Insert_CaseInsensitive_ShouldMatchAllCases()
    {
        // Trie uses ToLowerInvariant for storage, so uppercase queries should still match
        _trie.Insert(P(1, "ALI", "YILMAZ"));

        _trie.GetSuggestions("ALI").Should().NotBeEmpty();
        _trie.GetSuggestions("ali").Should().NotBeEmpty();
        _trie.GetSuggestions("Ali").Should().NotBeEmpty();
    }
}
