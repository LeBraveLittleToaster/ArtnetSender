namespace LumenForgeServer.Rentals.Domain.Actions
{
    public class CancelAction : StepAction
    {

    }

    public class CancelActionInput : SubmitActionInput
    {
        public string? Reason { get; set; }
    }

    public class CancelActionResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
    }
}
