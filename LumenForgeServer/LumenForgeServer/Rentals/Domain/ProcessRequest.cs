namespace LumenForgeServer.Rentals.Domain
{
    public class ProcessRequest
    {
        public long Id { get; set; }

        public Guid Guid { get; set; }

        Dictionary<string, string> Parameters { get; set; } = null!;
    }
}
