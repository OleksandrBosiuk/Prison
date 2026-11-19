using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Prison.Core
{
    public class SqlWorldStorage : IWorldStorage
    {
        private readonly string _connectionString;
        public string StorageName => "LocalDB Storage (ADO.NET)";

        public SqlWorldStorage(string connectionString)
        {
            _connectionString = connectionString;
        }

        private async Task EnsureDatabaseAndTablesAsync(CancellationToken token)
        {
            var builder = new SqlConnectionStringBuilder(_connectionString);
            string targetDb = builder.InitialCatalog;
            builder.InitialCatalog = "master";

            await using (var masterConn = new SqlConnection(builder.ConnectionString))
            {
                await masterConn.OpenAsync(token);
                string createDbSql = $"IF DB_ID(N'{targetDb}') IS NULL CREATE DATABASE [{targetDb}];";
                await using var cmd = new SqlCommand(createDbSql, masterConn);
                await cmd.ExecuteNonQueryAsync(token);
            }

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(token);

            string schemaSql = @"
                IF OBJECT_ID(N'dbo.PrisonBlocks', N'U') IS NULL
                CREATE TABLE dbo.PrisonBlocks (
                    BlockId INT IDENTITY(1,1) PRIMARY KEY,
                    BlockName NVARCHAR(50) NOT NULL UNIQUE
                );

                IF OBJECT_ID(N'dbo.Prisoners', N'U') IS NULL
                CREATE TABLE dbo.Prisoners (
                    Id UNIQUEIDENTIFIER PRIMARY KEY,
                    BlockId INT NOT NULL FOREIGN KEY REFERENCES dbo.PrisonBlocks(BlockId) ON DELETE CASCADE,
                    TypeName NVARCHAR(50) NOT NULL,
                    Name NVARCHAR(100) NOT NULL,
                    Energy INT NOT NULL CHECK (Energy >= 0 AND Energy <= 100)
                );";

            await using var schemaCmd = new SqlCommand(schemaSql, conn);
            await schemaCmd.ExecuteNonQueryAsync(token);
        }

        public async Task SaveAsync(WorldDto world, string destination, CancellationToken token = default)
        {
            await EnsureDatabaseAndTablesAsync(token);

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(token);
            await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(token);

            try
            {
                await using (var clearCmd = new SqlCommand("DELETE FROM dbo.Prisoners; DELETE FROM dbo.PrisonBlocks;", conn, tx))
                {
                    await clearCmd.ExecuteNonQueryAsync(token);
                }

                foreach (var block in world.Blocks)
                {
                    int blockId;
                    string insertBlockSql = "INSERT INTO dbo.PrisonBlocks (BlockName) OUTPUT INSERTED.BlockId VALUES (@bName);";
                    await using (var blockCmd = new SqlCommand(insertBlockSql, conn, tx))
                    {
                        blockCmd.Parameters.Add("@bName", SqlDbType.NVarChar, 50).Value = block.BlockName;
                        blockId = Convert.ToInt32(await blockCmd.ExecuteScalarAsync(token));
                    }

                    string insertInmateSql = @"
                        INSERT INTO dbo.Prisoners (Id, BlockId, TypeName, Name, Energy)
                        VALUES (@id, @blockId, @type, @name, @energy);";

                    foreach (var p in block.Inmates)
                    {
                        await using var inmateCmd = new SqlCommand(insertInmateSql, conn, tx);
                        inmateCmd.Parameters.Add("@id", SqlDbType.UniqueIdentifier).Value = p.Id;
                        inmateCmd.Parameters.Add("@blockId", SqlDbType.Int).Value = blockId;
                        inmateCmd.Parameters.Add("@type", SqlDbType.NVarChar, 50).Value = p.TypeName;
                        inmateCmd.Parameters.Add("@name", SqlDbType.NVarChar, 100).Value = p.Name;
                        inmateCmd.Parameters.Add("@energy", SqlDbType.Int).Value = p.Energy;
                        await inmateCmd.ExecuteNonQueryAsync(token);
                    }
                }

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
            await EnsureDatabaseAndTablesAsync(token);

            var world = new WorldDto { SavedAt = DateTime.UtcNow };
            var blockMap = new Dictionary<string, PrisonBlockDto>(StringComparer.OrdinalIgnoreCase);

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(token);

            string selectSql = @"
                SELECT b.BlockName, p.Id, p.TypeName, p.Name, p.Energy
                FROM dbo.PrisonBlocks b
                LEFT JOIN dbo.Prisoners p ON p.BlockId = b.BlockId
                ORDER BY b.BlockId;";

            await using var cmd = new SqlCommand(selectSql, conn);
            await using var reader = await cmd.ExecuteReaderAsync(token);

            while (await reader.ReadAsync(token))
            {
                string blockName = reader.GetString(0);
                if (!blockMap.TryGetValue(blockName, out var blockDto))
                {
                    blockDto = new PrisonBlockDto { BlockName = blockName };
                    blockMap[blockName] = blockDto;
                    world.Blocks.Add(blockDto);
                }

                if (!reader.IsDBNull(1))
                {
                    blockDto.Inmates.Add(new PrisonerDto
                    {
                        Id = reader.GetGuid(1),
                        TypeName = reader.GetString(2),
                        Name = reader.GetString(3),
                        Energy = reader.GetInt32(4)
                    });
                }
            }

            WorldValidator.Validate(world);
            return world;
        }
    }
}
