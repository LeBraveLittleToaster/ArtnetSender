using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Inventory.Dto.Update;

/// <summary>
/// Payload for updating category metadata.
/// </summary>
public sealed class UpdateCategoryDto : IValidatableObject
{
    /// <summary>
    /// Updated category name.
    /// </summary>
    [StringLength(256, MinimumLength = 1)]
    [RegularExpression(@".*\S.*")]
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>
    /// Updated category description.
    /// </summary>
    [StringLength(2000)]
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <summary>
    /// Ensures at least one field is provided.
    /// </summary>
    /// <param name="validationContext">Input value used by this operation.</param>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Name is null && Description is null)
        {
            yield return new ValidationResult(
                "At least one of Name or Description must be provided.",
                new[] { nameof(Name), nameof(Description) });
        }
    }
}
