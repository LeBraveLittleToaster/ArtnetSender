namespace LumenForgeServer.Rentals.Domain
{
    public class ProcessInstance
    {

        public long Id { get; set; }

        public Guid Guid { get; set; }

        public List<ProcessStep> Steps { get; set; } = null!;


    }
}
