using FluentAssertions;
using LumenForgeServer.Rentals.Dto.Query;
using System.Reflection;

namespace LumenForgeServer.UnitTests.Rentals.Controller;

/// <summary>
/// Tests the private <c>ParseIncludes</c> helper on <c>RentalOverviewController</c>.
/// Uses reflection to invoke the private static method.
/// </summary>
public class ParseIncludesTests
{
    private static RentalProcessInclude ParseIncludes(string? include)
    {
        var method = typeof(LumenForgeServer.Rentals.Controller.RentalOverviewController)
            .GetMethod("ParseIncludes", BindingFlags.NonPublic | BindingFlags.Static)!;

        return (RentalProcessInclude)method.Invoke(null, [include])!;
    }

    [Fact]
    public void Null_ReturnsNone()
        => ParseIncludes(null).Should().Be(RentalProcessInclude.None);

    [Fact]
    public void Empty_ReturnsNone()
        => ParseIncludes("").Should().Be(RentalProcessInclude.None);

    [Fact]
    public void Whitespace_ReturnsNone()
        => ParseIncludes("   ").Should().Be(RentalProcessInclude.None);

    [Fact]
    public void Checklists_ReturnsChecklistsFlag()
        => ParseIncludes("checklists").Should().Be(RentalProcessInclude.Checklists);

    [Fact]
    public void Extensions_ReturnsExtensionsFlag()
        => ParseIncludes("extensions").Should().Be(RentalProcessInclude.Extensions);

    [Fact]
    public void DamageReports_ReturnsDamageReportsFlag()
        => ParseIncludes("damage_reports").Should().Be(RentalProcessInclude.DamageReports);

    [Fact]
    public void All_ReturnsAllFlags()
        => ParseIncludes("all").Should().Be(RentalProcessInclude.All);

    [Fact]
    public void CommaSeparated_CombinesFlags()
        => ParseIncludes("checklists,extensions")
            .Should().Be(RentalProcessInclude.Checklists | RentalProcessInclude.Extensions);

    [Fact]
    public void CommaSeparatedWithSpaces_CombinesFlags()
        => ParseIncludes("checklists , damage_reports")
            .Should().Be(RentalProcessInclude.Checklists | RentalProcessInclude.DamageReports);

    [Fact]
    public void Unknown_ReturnsNone()
        => ParseIncludes("unknown").Should().Be(RentalProcessInclude.None);

    [Fact]
    public void MixedValidAndUnknown_ReturnsOnlyValid()
        => ParseIncludes("checklists,garbage,extensions")
            .Should().Be(RentalProcessInclude.Checklists | RentalProcessInclude.Extensions);

    [Fact]
    public void CaseInsensitive_ParsesCorrectly()
        => ParseIncludes("CHECKLISTS").Should().Be(RentalProcessInclude.Checklists);

    [Fact]
    public void AllThree_ReturnsAll()
        => ParseIncludes("checklists,extensions,damage_reports")
            .Should().Be(RentalProcessInclude.All);
}
