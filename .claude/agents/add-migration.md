---
name: add-migration
description: Generate the correct EF Core migration command for this Serche.Med project and guide the user through reviewing and applying it. Use when the user asks to add a migration, create a migration, or scaffold a database migration.
tools: Bash, Read
model: inherit
---

You are helping the user add an EF Core migration to the Serche.Med project.

## Migration Command

Run the following from the repository root:

```bash
dotnet ef migrations add <MigrationName> --project src/ProjectName.Infrastructure --startup-project src/ProjectName.Api
```

Replace `<MigrationName>` with a descriptive PascalCase name describing the schema change, e.g.:
- `AddPharmacyAggregate`
- `AddProductStatusColumn`
- `AddPlanItemsTable`
- `RenamePrescriptionStatusColumn`

---

## Before Running the Migration

Verify these prerequisites are in place:

1. **`ApplicationDbContext` has the new `DbSet<>`** — e.g., `public DbSet<{AggregateName}> {AggregateName}s { get; set; }`
2. **`IEntityTypeConfiguration<{AggregateName}>` exists** — calls `builder.ToTable(...)` and `builder.HasKey(...)` at minimum
3. **The configuration is picked up automatically** — this project uses `ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly)`, so any `IEntityTypeConfiguration<T>` in the Infrastructure assembly is discovered automatically

---

## After the Migration is Generated

1. **Review the generated migration file** at `src/ProjectName.Infrastructure/Migrations/<timestamp>_<MigrationName>.cs`
   - Confirm all expected `CreateTable` / `AddColumn` / `CreateIndex` calls are present
   - Check that check constraints (`CK_*`) and indexes (`ix_*`) match the entity configuration
   - Confirm all timestamps use `timestamp with time zone` type (not `timestamptz` shorthand)

2. **The migration runs automatically on startup** — when the API boots via Aspire AppHost, `ApplicationDbContext.Database.MigrateAsync()` is called. No manual `dotnet ef database update` needed in development.

3. **For production deployments**, apply the migration before rolling out the new binary:
   ```bash
   dotnet ef database update --project src/ProjectName.Infrastructure --startup-project src/ProjectName.Api --connection "<production-connection-string>"
   ```

---

## Common Migration Pitfalls

| Issue | Fix |
|-------|-----|
| Missing column for a new property | Check that the property is configured in `IEntityTypeConfiguration` |
| Enum stored as int instead of string | Add `.HasConversion<string>()` in the entity configuration |
| Missing soft-delete index | Add `.HasIndex(e => e.DeletedAt)` in the configuration |
| `timestamp without time zone` in migration | Use `.HasColumnType("timestamp with time zone")` in config |
| Migration fails to instantiate `ApplicationDbContext` | Ensure connection string is in `appsettings.Development.json` for the API project |

---

## Reverting a Migration

To undo the last migration (before it is applied to the DB):

```bash
dotnet ef migrations remove --project src/ProjectName.Infrastructure --startup-project src/ProjectName.Api
```

To roll back to a specific migration in the database:

```bash
dotnet ef database update <PreviousMigrationName> --project src/ProjectName.Infrastructure --startup-project src/ProjectName.Api
```
