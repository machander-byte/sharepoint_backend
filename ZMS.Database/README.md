# ZMS Database

This folder contains the starter SQL schema for the migration suite.

- `Scripts/001_initial_schema.sql` creates the `Connections`, `MigrationJobs`, `MigrationItems`, and `Logs` tables.
- The API uses EF Core and `Database.EnsureCreated()` for a first run experience, but this SQL file is included so the database shape is explicit and easy to evolve into formal migrations.
