using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LumenForgeServer.Common.Database;

/// <summary>
/// Shared model-builder extensions for module configuration and naming conventions.
/// </summary>
public static class  ModelBuilderExtensions
{
    /// <summary>
    /// Applies all module configuration classes from this assembly.
    /// </summary>
    /// <param name="builder">Model builder used to configure entity mappings.</param>
    public static void ApplyModuleConfigurations(this ModelBuilder builder)
    {
        builder.ApplyConfigurationsFromAssembly(
            typeof(AppDbContext).Assembly,
            type =>
                type.Namespace?.Contains(".Persistence.Configurations", StringComparison.Ordinal) == true ||
                type.Namespace?.Contains(".Persistance.Configurations", StringComparison.Ordinal) == true);
    }

    
}
