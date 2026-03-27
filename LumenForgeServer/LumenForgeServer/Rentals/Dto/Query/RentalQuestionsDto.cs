using System.Text.Json.Serialization;
using LumenForgeServer.Rentals.Dto.View;

namespace LumenForgeServer.Rentals.Dto.Query;

public class RentalQuestionsDto
{
    [JsonPropertyName("questions")]
    public List<QuestionView> Questions{ get; set; }
}