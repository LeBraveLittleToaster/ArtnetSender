using LumenForgeServer.Rentals.Domain;

namespace LumenForgeServer.Common.Database.Seeding.Seeders;

/// <summary>
/// Seeds survey questions from the embedded questions CSV.
/// Runs in all environments; idempotent — skips if questions already exist.
/// </summary>
public class QuestionSeeder(AppDbContext db) : IDataSeeder
{
    public int Order => 50;
    public SeedEnvironment Environment => SeedEnvironment.All;

    /// <summary>
    /// Executes the seed async operation.
    /// </summary>
    /// <remarks>Potential side effects: may modify state as part of this operation.</remarks>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SeedAsync(CancellationToken ct)
    {
        if (db.Questions.Any())
            return;

        foreach (var row in SeedDataLoader.Load("questions.csv"))
        {
            if (row.Length < 5) continue;

            try
            {
                QuestionDataType.TryParse(row[4].Trim(), out QuestionDataType questionDataType);
                db.Questions.Add(new Question
                {
                    Guid         = Guid.NewGuid(),
                    QuestionText = row[0].Trim(),
                    Category     = row[1].Trim(),
                    DisplayOrder = int.Parse(row[2].Trim()),
                    IsActive     = bool.Parse(row[3].Trim()),
                    QuestionDataType = questionDataType 
                });
            }catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            
        }

        await db.SaveChangesAsync(ct);
    }
}
