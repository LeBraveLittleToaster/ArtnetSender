namespace LumenForgeServer.Rentals.Domain
{
    public class ProcessStep
    {

        public long Id { get; set; }
        public Guid Guid { get; set; }

        public StepType StepType { get; set; } = new();
        public List<Companion> InputCompanions { get; set; } = null!;

        public List<Companion> EmittedCompanions { get; set; } = null!;

        public StepAction RentalAction { get; set; } = null!;
    }
}
