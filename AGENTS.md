## Constraints

- Never read or analyze `appsettings.json` or `module-name.settings.json`.
- Do not add NuGet packages, run database scripts, or run migrations without explicit user approval.
- Do not log secrets, credentials, access or refresh tokens, authorization headers, cookies, connection strings,
  personally identifiable data, or complete request/response payloads. Use allowlisted, redacted structured log fields;
  error logs expose safe correlation/context rather than sensitive exception or payload data.

## Language and communication

- Before communicating or generating documentation, comments, or pull request content, read `parameters.language` from
  `settings.yaml` (usually `.ai/settings.yaml` in consumer projects). Use
  `parameters.language.communication` for developer communication, `parameters.language.files` for generated or
  modified files and documentation, `parameters.language.pull_request.comments` for pull request comments,
  `parameters.language.pull_request.descriptions` for pull request descriptions, and
  `parameters.language.pull_request.titles` for pull request titles.
- Follow the configured languages; do not hard-code them. Keep code, identifiers, API names, schema keys, and other
  tool/format syntax unchanged.
- Pull request titles follow the commit convention: `type(#issue): description`. The description starts with a
  lowercase letter, including when its configured language normally capitalizes titles.

## Architecture and layout

- Modular monolith: each `moduleName` owns its domain/persistence; `SolutionName.Bootstrapper` is the entry
  point/composition root; cross-module flows use choreography and ACL ports, never bootstrapper orchestration. Shared
  projects contain technical infrastructure/common contracts.
- Read `docs/foundation/prd.md` (product ownership/scope) and `docs/foundation/tech-stack.md` (global decisions);
  module rules override global rules.
- Rich modules: `Acl`, `Api`, `Application`, `Domain`, `Infrastructure/Persistence/Migrations`; group domain code by
  type (`Entities`, `ValueObjects`, `Dictionaries`, `Policies`, `Services`, `Exceptions`, `Repositories`). Simple
  CRUD: `Api` and `Core`. Module docs: `src/moduleName/docs/`.

### Folder tree

```text
src/
├── moduleName/
│   ├── moduleName.Acl/                    # cross-module contracts/adapters
│   ├── moduleName.Api/                    # minimal API endpoints
│   ├── moduleName.Application/            # commands, queries, handlers
│   ├── moduleName.Domain/                 # entities, value objects, policies, etc.
│   ├── moduleName.Infrastructure/         # persistence and external services
│   │   └── Persistence/Migrations/
│   ├── moduleName.Core/                   # simple CRUD modules only
│   └── docs/                              # module PRD, db plan, scenarios
├── Shared/                                # shared infrastructure/contracts
├── SolutionName.Bootstrapper/             # entry point/composition root
└── Front/                                 # frontend projects
tests/
└── moduleName/SolutionName.moduleName.Layer.Tests/
```

## Required documentation

Before changing a module, read each existing item in `src/moduleName/docs/foundation/prd.md`,
`src/moduleName/docs/foundation/db-plan.md`, `src/moduleName/docs/foundation/delivery-order.md`,
`docs/scenarios/feature_<feature_key>/` (parent feature and end-to-end Gherkin), and
`src/{Module}/docs/scenarios/feature_<feature_key>/` (module subissue and module Gherkin).

## Architecture Decision Records

- ADRs are stored as folders in `docs/adr/`. Before architectural work, read every `docs/adr/*/summary.md` to learn the current decisions without loading their full context.
- Read an ADR's `details.md` only when its summary is relevant to the task or when the decision needs to be applied, verified, or reconsidered.

## Delivery and GitHub

- CI/CD is in `.github/workflows/`; hosting/deployment is provider-specific and configured in repository files.
- GitHub repository/project settings are in `.ai/settings.yaml` (`parameters.github.*`). Before API operations run
  `gh api user --jq .login`; if networking is sandbox-blocked, use the approved external path. Never print, store, or
  commit tokens.
- Issue creation: check duplicates; add `feature`, `backlog`, and module labels; run `gh project item-add`; set status
  with `gh project item-edit`; sync the ID in the PRD and `docs/scenarios/issue_tracker.yaml` for parent features or
  the feature-local `issue_tracker.yaml` for module subissues. Module scenarios live in
  `src/{Module}/docs/scenarios/feature_<feature_key>/`.

## Code style

### Frontend

- Blazor: use code-behind; do not put `data-testid` in markup—use `ITestIdProvider.Get(id)`.
- Use bUnit and the backend integration tests' Gherkin layout and naming rules.

### .NET backend

- NRT is disabled: do not add `<Nullable>enable</Nullable>` or `#nullable enable`.
- Prefer minimal APIs that inject Wolverine's `IMessageBus` and call `InvokeAsync<TResult>(query, cancellationToken)`
  for queries and `InvokeAsync(command, cancellationToken)` (or `InvokeAsync<TResult>` for a result) for commands.
- Endpoints use `IRequest` records from `Requests`, call `Validate()`, return `ApiResponse`/`ApiResponse<T>`, and map
  `*Request` to unsuffixed `IQuery`/`ICommand`. Authentication may replace validation errors with generic responses
  to avoid leaking account/format validity.
- DI: scoped for request-specific services, singleton for stateless services. Always use braces; never add `Async` to
  method names.
- Commands/queries are records in the feature's `Commands`/`Queries` folder; commands implement `ICommand` or
  `ICommand<TResult>`, queries `IQuery<TResult>` (`moduleName.Application/Abstractions`). Queries
  do not mutate state or dispatch commands. Handlers orchestrate loading, ACL mapping, persistence, and publication;
  aggregates/policies own state transitions/business rules. No local facade classes ending in `Module`; expose modules
  only through narrow ACL contracts/adapters.
- Load entities by ID; a missing query entity raises `NotFoundException(id)`. Keep each command or query record and
  its handler in one file named after the record (`AddTile.cs` holds `AddTile` and `AddTileHandler`).
- JSONB aggregates use `PrivateFieldsAndPropertiesSerializer`: no computed get-only properties or backing-field/public
  view pairs. Use private-set auto-properties and a parameterized private factory constructor; put the obsolete,
  parameterless serialization constructor last. Verify real serializer round-trips.

## Persistence and EF Core

- If `db-plan.md` exists, use EF Core with the Npgsql provider and follow it; otherwise use an in-memory static
  `Dictionary` and never infer a schema. Do not use Dapper. Raw SQL is a last resort and must be parameterized
  (`FromSql`/`ExecuteSql` with interpolation, never string concatenation).
- A repository is a DDD building block: its interface belongs to the domain and expresses collection-like access to
  aggregate roots. Keep repository implementations in the infrastructure layer. Do not use aggregate repositories
  for read-model queries; give those queries dedicated read interfaces instead.
- Each module owns one `moduleNameDbContext` in `moduleName.Infrastructure/Persistence/`; never share a context or
  map another module's tables. Schema = lowercase `snake_case` module name, set with `HasDefaultSchema`.
- Map every table with its own `IEntityTypeConfiguration<T>` in `Persistence/Configurations/`, applied through
  `ApplyConfigurationsFromAssembly`. Name tables, columns, keys and constraints explicitly as in `db-plan.md`
  (`pk_*`, `fk_*`, `ix_*`, `uq_*`, `ck_*`): column types, composite keys, `OnDelete`, unique constraints
  (`HasAlternateKey`), unique indexes, CHECK (`HasCheckConstraint`) and SQL defaults (`HasDefaultValueSql`).
- EF entities are persistence row models in `Persistence/Rows/`, separate from domain objects; repositories map rows
  to aggregates and back. Never expose `DbContext`, `DbSet` or `IQueryable` outside infrastructure.
- Register the context with `AddDbContextFactory` over the module's `NpgsqlDataSource`. Singleton repositories create
  a short-lived context per operation from `IDbContextFactory<moduleNameDbContext>`. Reads use `AsNoTracking` and
  ordered `Include`s (`AsSplitQuery` for several collections); bulk changes use `ExecuteUpdate`/`ExecuteDelete`.
- Migrations are EF Core migrations in `Persistence/Migrations/` with the history table in the module schema
  (`MigrationsHistoryTable("__ef_migrations_history", schema)`). Creating migration files is allowed; applying them
  (`dotnet ef database update`, `Database.Migrate()`, generated scripts) requires explicit user approval.
- Aggregate repositories own one transaction: persist the aggregate and mapped integration events to the PostgreSQL
  outbox in the same context/transaction, then commit once. Before commit, do not publish, call another module,
  load cross-module state, or clear pending domain events. Pass ACL-loaded persistence context in.

## CQRS with Wolverine

- Wolverine (`WolverineFx`) is the mediator; do not add custom dispatchers, MediatR or Scrutor registration.
  `moduleName.Application` has no Wolverine reference: handlers are plain classes found by convention.
- A handler is a `public sealed class <Record>Handler` with one `Handle` method taking the record (plus an optional
  `CancellationToken`). It returns the result directly (`TResult`, `Task<TResult>`, `void` or `Task`); dependencies come
  through the primary constructor. Do not name handler methods `HandleAsync` or `Consume`.
- Logic shared by several handlers lives in an application component (for example `TilePreviewer`) or a static
  helper, never in a nested `IMessageBus` call from a handler.
- The bootstrapper calls `builder.Host.UseWolverine(...)` and includes every module's Application assembly with
  `options.Discovery.IncludeAssembly(...)`. `WolverineFx` belongs to the API project, `WolverineFx.RuntimeCompilation`
  to the bootstrapper (runtime code generation).
- Exceptions thrown by handlers propagate from `InvokeAsync` to the API exception handler; do not add Wolverine retry
  or error policies to inline invocations unless a scenario requires them.
- Integration tests reuse one application host per test assembly, because every host compiles Wolverine handler
  code. Reset all in-memory fakes and mutable options before each test instead of creating a new host.

## Events and outbox

- Domain events implement `IDomainEvent` from `SolutionName.Shared.Kernel`, stay in their module, and are inspected by
  repositories while saving. Convert only boundary-crossing facts via module-local typed extensions/factories; skip
  local events; clear them only after the aggregate/outbox transaction commits.
- Integration events implement `IEvent` from `SolutionName.Shared.Infrastructure.Events.Integration`, contain no domain
  objects, and expose non-empty `Guid Id`/`Guid SubjectId`. Create `Id` once and retain it as the outbox identifier
  across serialization/retries. `SubjectId` is trusted background identity; never include HTTP context, access tokens,
  or role snapshots.
- Delivery model: notification (ID only) or carried state (default). Each module may register a local type for the same
  event name. The event dictionary deserializes and dispatches matching handlers one after another because they share
  one scope and its per-module `DbContext`. A failing handler does not stop the others; rethrow collected
  failures so the message is retried.
- The integration publisher always writes to the transactional PostgreSQL outbox in the bootstrapper's `outbox` schema;
  production has no in-memory outbox. `OutboxBackgroundService` delivers outside HTTP requests and restores `SubjectId`
  to scoped execution context. Acknowledge only after all handlers succeed; handlers must be idempotent. At-least-once
  delivery: retry at most three times, then dead-letter. Use one-minute leases, 20-second renewals, compare-and-set
  ownership, and isolate per-message lease/state failures from the worker.

## Tests

- Use NUnit. Domain tests instantiate real domain objects and do not mock. Integration tests use custom in-memory
  dependencies, never mocking libraries, and derive from the module Gherkin in
  `src/{Module}/docs/scenarios/feature_<feature_key>/gherkin.md`; end-to-end tests may use the parent Gherkin in
  `docs/scenarios/feature_<feature_key>/gherkin.md`.
- Each module contribution has `subissue.md` and `gherkin.md`; each test uses
  `[Test(Description = "<feature-or-subissue-folder> - #<step>")]` and `Given_When_Then` naming. Do not add comments
  above `[Test]` or in test bodies.
- Parent feature and module subissue folders share one name, so a description alone cannot identify a step's Gherkin.
  For parent scenarios or tests covering parent plus module scenarios, use `[Test]` plus `[Scenarios(...)]` from
  `SolutionName.Testing`, referencing the parent by feature key and each module by subissue key:
  `[Scenarios("feature_join_challenge_end_to_end - #3", "front-participation-flow - #1")]`.
  This joins references into the test description and exposes each as a `Scenario` property, so
  `NUnit.Where="Scenario == '<reference>'"` selects every covering test. Keep single-reference module tests on
  `[Test(Description = ...)]`.

## Commits

- Format: `feat(#123): Add ...`, `fix(#123): Fix ...`, `fix: Fix ...`, `refactor(#123): Refactor ...`,
  `refactor: Refactor ...`, or `docs: Update PRD for moduleName module`.
- Use an imperative description; require an issue for `feat`, allow it for `fix`/`refactor`, and add no body.

## Common commands

```bash
dotnet build
dotnet build SolutionName.sln
dotnet run --project src/SolutionName.Bootstrapper/SolutionName.Bootstrapper.csproj
dotnet watch --project src/SolutionName.Bootstrapper/SolutionName.Bootstrapper.csproj
dotnet test
dotnet test SolutionName.ci-unit.slnf
dotnet test --filter "FullyQualifiedName~<TestClassName>"
dotnet sln SolutionName.sln add <path/to/Project.csproj>
dotnet new classlib -n <ProjectName> -o <path>
dotnet restore
```
