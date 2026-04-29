---
name: add-ef-configuration
description: Scaffold an EF Core IEntityTypeConfiguration<T> for a new aggregate or entity following the codebase conventions — application schema, check constraints, enum-as-string, timestamp columns, soft-delete index, relationships. Use when the user asks to add or complete the EF Core mapping for a domain entity.
allowed-tools: Read Write Glob Grep
---

You are an expert in this codebase's EF Core mapping conventions. Given an aggregate (or child entity) and its properties, generate the configuration class and register it via the existing `ApplyConfigurationsFromAssembly` mechanism (no extra registration needed).

## User's Request

$ARGUMENTS

---

## What to Generate

1. `src/ProjectName.Infrastructure/Database/Configurations/{AggregateName}Configuration.cs`

If a child entity also needs mapping, create a separate `{ChildEntity}Configuration.cs` in the same folder.

---

## Configuration Class Template

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectName.Domain.AggregatesModel.{Domain}Aggregate;

namespace ProjectName.Infrastructure.Database.Configurations;

public class {AggregateName}Configuration : IEntityTypeConfiguration<{AggregateName}>
{
    public void Configure(EntityTypeBuilder<{AggregateName}> builder)
    {
        builder.ToTable("{AggregateName}s", ApplicationDbContext.ApplicationSchema, tableBuilder =>
        {
            // Check constraints mirror domain validation invariants
            tableBuilder.HasCheckConstraint(
                "CK_{AggregateName}s_Name_NotEmpty",
                "\"Name\" <> ''");

            // Enum check constraint — list all valid string values
            tableBuilder.HasCheckConstraint(
                "CK_{AggregateName}s_Status_Enum",
                "\"Status\" IN ('Active', 'Inactive', 'Suspended')");
        });

        // Primary key
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        // Required string with max length — use aggregate constant
        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength({AggregateName}.NameMaxLength);

        // Optional string with max length
        builder.Property(x => x.Description)
            .HasMaxLength({AggregateName}.DescriptionMaxLength);

        // Enum stored as string
        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        // Audit timestamps — ALWAYS use "timestamp with time zone"
        builder.Property(x => x.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.ModifiedAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        // Soft-delete timestamp — always nullable
        builder.Property(x => x.DeletedAt)
            .IsRequired(false)
            .HasColumnType("timestamp with time zone");

        // Relationships (examples — adapt to actual model)
        // One-to-many to parent:
        // builder.HasOne(x => x.Parent)
        //     .WithMany(x => x.Children)
        //     .HasForeignKey(x => x.ParentId)
        //     .OnDelete(DeleteBehavior.Cascade);

        // One-to-many owned collection:
        // builder.HasMany(x => x.Items)
        //     .WithOne(x => x.{AggregateName})
        //     .HasForeignKey(x => x.{AggregateName}Id)
        //     .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(x => x.Name)
            .HasDatabaseName("ix_{aggregateName_lower}s_name");

        // Always add an index on DeletedAt for soft-deletable tables
        builder.HasIndex(x => x.DeletedAt)
            .HasDatabaseName("ix_{aggregateName_lower}s_deleted_at");
    }
}
```

---

## Property Type Conventions

| Property type | Configuration |
|---|---|
| `string` required | `.IsRequired().HasMaxLength({Aggregate}.PropMaxLength)` |
| `string` optional | `.HasMaxLength({Aggregate}.PropMaxLength)` |
| `enum` | `.IsRequired().HasConversion<string>().HasMaxLength(32)` |
| `DateTime` / `DateTimeOffset` audit | `.IsRequired().HasColumnType("timestamp with time zone")` |
| `DateTimeOffset?` DeletedAt | `.IsRequired(false).HasColumnType("timestamp with time zone")` |
| `decimal` money | `.HasColumnType("numeric(18,2)")` |
| `int[]` or `List<int>` (Postgres array) | `.HasColumnType("integer[]")` |
| JSON column | `.HasColumnType("jsonb").HasDefaultValueSql("'[]'::jsonb")` |
| `bool` | `.IsRequired()` (default, no extra config needed) |
| `Guid` | `.IsRequired()` (auto-mapped to `uuid`) |

## Check Constraint Naming

Format: `CK_{TableName}_{ColumnName}_{RuleType}`

Examples:
- `CK_Pharmacies_Name_NotEmpty` → `"Name" <> ''`
- `CK_Pharmacies_Status_Enum` → `"Status" IN ('Active', 'Inactive')`
- `CK_Plans_TotalPrice_Positive` → `"TotalPrice" >= 0`

Always add check constraints for:
1. Required string columns — `NOT EMPTY`
2. Enum columns — `IN (...)` listing all valid `string` values

## Index Naming

Format: `ix_{table_lower}_{columns_lower}`

Examples:
- `ix_pharmacies_name`
- `ix_prescriptions_deleted_at`
- `ix_prescriptions_user_id_deleted_at` (composite)

**Always create an index on `DeletedAt`** for every soft-deletable table.

## Table and Schema

- Always use `ApplicationDbContext.ApplicationSchema` (`"application"`) — never the `"public"` schema
- Table name is the aggregate class name pluralized (e.g., `Pharmacy` → `"Pharmacies"`)

## Max-Length Source

Max-length values **must** come from constants defined on the domain aggregate:

```csharp
builder.Property(x => x.Name).HasMaxLength({AggregateName}.NameMaxLength);
```

Never hardcode numeric values like `.HasMaxLength(200)`.

## Checklist Before Finishing

- [ ] Table is in `ApplicationDbContext.ApplicationSchema`
- [ ] All required strings have `HasCheckConstraint` for `<> ''`
- [ ] All enum columns have `HasConversion<string>()` and an `IN (...)` check constraint — values match the enum members exactly as strings
- [ ] All timestamp properties use `"timestamp with time zone"` column type
- [ ] `DeletedAt` is `IsRequired(false)`
- [ ] Index on `DeletedAt` exists (`ix_{table}s_deleted_at`)
- [ ] Max-length values reference aggregate constants, not magic numbers
- [ ] All navigation properties have explicit `HasOne/HasMany` + `HasForeignKey` + `OnDelete`
- [ ] Index names follow `ix_{table_lower}_{column_lower}` pattern
