using Microsoft.EntityFrameworkCore;

namespace Prison.Core
{
    public class PrisonDbContext : DbContext
    {
        public PrisonDbContext(DbContextOptions<PrisonDbContext> options)
            : base(options) { }

        public DbSet<Prisoner> Prisoners => Set<Prisoner>();
        public DbSet<Rookie> Rookies => Set<Rookie>();
        public DbSet<Philosopher> Philosophers => Set<Philosopher>();
        public DbSet<Artist> Artists => Set<Artist>();
        public DbSet<Lawyer> Lawyers => Set<Lawyer>();
        public DbSet<Athlete> Athletes => Set<Athlete>();
        public DbSet<Mechanic> Mechanics => Set<Mechanic>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var prisoner = modelBuilder.Entity<Prisoner>();
            prisoner.ToTable("Prisoners");
            prisoner.HasKey(p => p.Id);

            prisoner.Property(p => p.Name)
                .HasMaxLength(100)
                .IsRequired();

            prisoner.Property(p => p.BlockName)
                .HasMaxLength(50)
                .IsRequired();

            prisoner.Property(p => p.Energy)
                .IsRequired();

            prisoner.HasDiscriminator<string>("PrisonerKind")
                .HasValue<Rookie>("Rookie")
                .HasValue<Philosopher>("Philosopher")
                .HasValue<Artist>("Artist")
                .HasValue<Lawyer>("Lawyer")
                .HasValue<Athlete>("Athlete")
                .HasValue<Mechanic>("Mechanic");
        }
    }
}
