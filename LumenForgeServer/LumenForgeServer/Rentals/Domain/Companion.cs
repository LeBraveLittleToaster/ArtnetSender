using NodaTime;

namespace LumenForgeServer.Rentals.Domain
{
    public abstract class Companion
    {
        public long Id { get; set; }
        public Guid Uuid { get; set; }

        public Instant ExecutedAt { get; set; }
        public Instant CreatedAt { get; set; }
    }
}
