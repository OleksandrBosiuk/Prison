using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Prison.Core
{
    public class EfCoreWorldStorage : IWorldStorage
    {
        private readonly IDbContextFactory<PrisonDbContext> _factory;
        public string StorageName => "EF Core Storage (LocalDB + TPH)";

        public EfCoreWorldStorage(IDbContextFactory<PrisonDbContext> factory)
        {
            _factory = factory;
        }

        public async Task SaveAsync(WorldDto world, string destination, CancellationToken token = default)
        {
            WorldValidator.Validate(world);

            await using var db = await _factory.CreateDbContextAsync(token);
            await using var tx = await db.Database.BeginTransactionAsync(token);

            try
            {
                var existing = await db.Prisoners.ToListAsync(token);
                db.Prisoners.RemoveRange(existing);
                await db.SaveChangesAsync(token);

                foreach (var block in world.Blocks)
                {
                    foreach (var dto in block.Inmates)
                    {
                        Prisoner entity = dto.TypeName switch
                        {
                            "Philosopher" => new Philosopher(dto.Name),
                            "Artist" => new Artist(dto.Name),
                            "Lawyer" => new Lawyer(dto.Name),
                            "Athlete" => new Athlete(dto.Name),
                            "Mechanic" => new Mechanic(dto.Name),
                            _ => new Rookie(dto.Name)
                        };

                        entity.RestoreState(dto.Id, dto.Energy);
                        entity.BlockName = block.BlockName;
                        db.Prisoners.Add(entity);
                    }
                }

                await db.SaveChangesAsync(token);
                await tx.CommitAsync(token);
            }
            catch
            {
                await tx.RollbackAsync(token);
                throw;
            }
        }

        public async Task<WorldDto> LoadAsync(string source, CancellationToken token = default)
        {
            await using var db = await _factory.CreateDbContextAsync(token);

            var allEntities = await db.Prisoners
                .AsNoTracking()
                .ToListAsync(token);

            var world = new WorldDto { SavedAt = DateTime.UtcNow };
            var blockNames = new[]
            {
                "RookieBlock", "PhilosopherBlock", "ArtistBlock",
                "LawyerBlock", "AthleteBlock", "MechanicBlock"
            };

            foreach (var bName in blockNames)
            {
                var inmatesInBlock = allEntities
                    .Where(p => string.Equals(p.BlockName, bName, StringComparison.OrdinalIgnoreCase))
                    .Select(p => new PrisonerDto
                    {
                        Id = p.Id,
                        TypeName = p.TypeName,
                        Name = p.Name,
                        Energy = p.Energy
                    })
                    .ToList();

                world.Blocks.Add(new PrisonBlockDto
                {
                    BlockName = bName,
                    Inmates = inmatesInBlock
                });
            }

            WorldValidator.Validate(world);
            return world;
        }
    }
}
