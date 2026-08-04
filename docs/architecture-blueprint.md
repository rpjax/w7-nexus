# Nexus Architecture Blueprint

This document defines the default architecture blueprint for backend domains in `Nexus.Api`.

It is intended to be:

- practical enough to copy and apply immediately
- stable enough to scale across many domains
- hexagonal by default
- explicit about roles
- compatible with event sourcing without forcing it everywhere
- production-minded without ceremony for its own sake

If a new domain is created and there is no strong reason to do otherwise, follow this document.

## Official Status

This document is the **default architecture standard** for new `Nexus.Api` domains and for any code being substantially touched.

- Follow it for greenfield work.
- Treat existing code that diverges as debt to migrate opportunistically, not as the model to copy.
- Deviations need an explicit reason.

## Core Principles

1. Domain is the structural root.
2. Roles organize application entry points, not the domain model.
3. Aggregates protect consistency and business invariants.
4. Event sourcing is a persistence strategy for specific aggregates, not a root architectural axis.
5. Read concerns and write concerns are organized separately, even when full CQRS is not adopted.
6. Infrastructure is replaceable; domain behavior is not.
7. Start minimal; grow folders and abstractions only when a real need appears.
8. Bounded contexts must not leak infrastructure across domain boundaries.

## The One Default Shape

Every domain should follow this top-level structure:

```text
<Domain>/
  Domain/
  Application/
  Infrastructure/
  Presentation/
  Composition/
```

This is the default blueprint for `Accounts`, `Operations`, `Payments`, `Scripts`, and future domains.

## Roles and Domain Boundaries

The domain answers: "what business concept exists and what rules keep it valid?"

Roles answer: "who is using this domain, through which use cases, and under which authorization rules?"

Examples:

- `Payments` is a domain.
- `Accounts` is a domain.
- `Operator`, `Administrator`, and `TeamLeader` are roles.

Correct:

```text
Payments/
  Application/
    Ports/
      In/
        Administrator/
        Operator/
```

Incorrect:

```text
Operator/
  Payments/
Administrator/
  Payments/
```

Do not make roles the structural root. That duplicates the business model and causes the same concepts to be split across persona trees.

## Domain Templates

There are exactly **two** templates:

1. **Minimum viable domain** — create this by default.
2. **Optional additions** — add individual pieces only when a concrete need appears.

Do not create every folder “just in case”. Empty precautionary folders are ceremony.

### 1. Minimum viable domain (default)

```text
<Domain>/
  Domain/
    Aggregates/
      <AggregateName>/
        <AggregateName>.cs
        <AggregateName>Id.cs
    Errors/

  Application/
    Ports/
      In/
        <Role>/
          Commands/
            I<CreateSomething>UseCase.cs
          Queries/
            I<GetSomething>UseCase.cs
      Out/
        Persistence/
          I<AggregateName>Repository.cs
          I<AggregateName>ReadRepository.cs
        Identity/
          IRequestContext.cs
    UseCases/
      <Role>/
        Commands/
          <CreateSomething>/
            <CreateSomething>Command.cs
            <CreateSomething>Handler.cs
            <CreateSomething>Result.cs
        Queries/
          <GetSomething>/
            <GetSomething>Query.cs
            <GetSomething>Handler.cs
            <GetSomething>Result.cs
    Authorization/
      <Role>/

  Infrastructure/
    Persistence/
      Repositories/
      Records/
      Mapping/
      ReadModels/

  Presentation/
    Http/
      <Role>/
        <Domain><Role>Controller.cs
        Contracts/

  Composition/
    <Domain>ServiceCollectionExtensions.cs
```

### 2. Optional additions (only when needed)

Add these one by one when justified. Do not scaffold the whole list.

| Need | Add |
|---|---|
| Richer aggregate modeling | `Domain/Aggregates/<Name>/{State,ValueObjects,Rules,Events}` |
| Cross-aggregate domain logic | `Domain/Services/`, `Domain/Policies/`, `Domain/Events/` |
| External system calls | `Application/Ports/Out/External/`, `Infrastructure/External/` |
| Domain / integration messaging | `Application/Ports/Out/Messaging/`, `Application/Integration/`, `Infrastructure/Messaging/` |
| Clock / time abstraction | `Application/Ports/Out/Time/IClock.cs` |
| Mapping helpers / shared app DTOs | `Application/Mapping/`, `Application/DTOs/` |
| Event sourcing for an aggregate | `Infrastructure/Persistence/EventSourcing/{EventStore,Projections,Snapshots}` and optionally rename write repos under `StateBased/` |
| Background work | `Presentation/Jobs/`, `Presentation/Consumers/` |

### Anti-ceremony rule

- Do not create empty `Events/`, `EventSourcing/`, `Projections/`, `Snapshots/`, `Jobs/`, or `Consumers/` folders preemptively.
- Prefer `Infrastructure/Persistence/Repositories/` until event sourcing is actually adopted for an aggregate.
- Prefer grouped authorization policies until a capability-specific policy is clearly justified.

## Inter-Domain Dependency Rules

Bounded contexts must stay modular.

### Allowed

- Domain A may call Domain B through an explicit application port or anti-corruption adapter.
- Domain A may consume published integration contracts from Domain B.
- Shared kernel usage only for truly shared primitives (IDs, money, clock), never for business aggregates.

### Forbidden

- Domain A must never reference `Infrastructure` of Domain B.
- Domain A should not take a hard dependency on Domain B aggregates inside Application handlers as the default pattern.
- Do not reach into another domain’s Mongo records, mappers, or repositories.

### Preferred integration styles

1. Output port in A that B implements (or an adapter implements).
2. Integration events published by B and handled by A.
3. Explicit anti-corruption adapter that maps B’s model into A’s local model.

Existing cross-aggregate `using` across domains (for example Application code depending directly on another domain’s aggregates) is treated as debt to avoid in new code.

## Layer Responsibilities

### Domain

`Domain/` contains the business model and the rules that make it valid.

Allowed:

- aggregates
- entities
- value objects
- domain services
- business policies
- domain events
- business errors

Not allowed:

- controllers
- HTTP contracts
- database records
- event store details
- MongoDB, SQL, or framework persistence logic
- authorization logic based on transport or caller identity

### Application

`Application/` contains use cases, orchestration, ports, role-based entry points, and authorization policies.

Allowed:

- input ports
- output ports
- commands and queries
- use case handlers
- authorization policies
- request context usage
- mapping between application contracts and domain behavior
- translation of domain errors into application results

Not allowed:

- concrete persistence logic
- HTTP-specific request models
- framework-specific controllers
- infrastructure-level serialization or storage details

### Infrastructure

`Infrastructure/` contains adapters for persistence, event stores, external systems, projections, and messaging.

Allowed:

- repository implementations
- event store implementations
- read model implementations
- projection handlers
- integration gateways
- message publishing implementations

It should not define the core business rules of the domain.

### Presentation

`Presentation/` contains inbound adapters such as HTTP controllers, jobs, and consumers.

Allowed:

- controllers
- HTTP request and response contracts
- transport-level translation
- status code mapping from `IOperationResult`
- authentication/authorization wiring at the edge

Not allowed:

- important business rules
- direct persistence behavior
- aggregate mutation outside of use cases

## Result and Error Contract

Application handlers use the project’s existing result pattern. Do not invent a parallel Result framework.

### Official application contract

- Use case handlers return `IOperationResult<T>` (Aidan / project patterns already in use).
- Where internal helpers return `IResult<T>`, Application translates them into `IOperationResult<T>` at the use-case boundary.
- Presentation maps `IOperationResult<T>` to HTTP status codes and response bodies.
- Domain expresses failures as domain errors or failed domain results; Application owns the translation to the application result contract.

### `*Result.cs` vs `IOperationResult<T>`

These are different layers:

| Artifact | Role |
|---|---|
| `RefundPaymentResult.cs` | Success **payload** type (`T`) — what the use case returns when it succeeds |
| `IOperationResult<RefundPaymentResult>` | Application **envelope** — success or failure with errors |

Rules:

- Name files like `RefundPaymentResult.cs` for the payload.
- Handler / input port signature returns `Task<IOperationResult<RefundPaymentResult>>` (or `IOperationResult<T>` with another clear `T` when a dedicated payload type is unnecessary).
- Do not treat `*Result.cs` as a second Result monad. It is just the success data shape.
- Prefer a dedicated `*Result` type when the success payload has meaningful fields; use a simple type (`bool`, `string`, existing details DTO) when a dedicated type adds no value.

### Flow

```text
Domain error / invariant failure
  -> Application handler maps to IOperationResult failure
  -> Presentation maps to HTTP (Problem Details / API error shape)

Domain success
  -> Application returns IOperationResult.Success(payload)
  -> Presentation maps payload to HTTP response contract
```

## Official Naming Conventions

Use these names by default.

### Aggregates

- `Payment`
- `Account`
- `Operation`

### Aggregate IDs

- `PaymentId`
- `AccountId`
- `OperationId`

### Input Ports

- `IRefundPaymentUseCase`
- `IGetPaymentUseCase`
- `ISearchPaymentsUseCase`

### Use Case Models

- `RefundPaymentCommand`
- `GetPaymentQuery`
- `RefundPaymentResult` (success payload `T`, not the result monad)
- `GetPaymentResult`

### Use Case Implementations

- `RefundPaymentHandler` implements `IRefundPaymentUseCase` and returns `IOperationResult<RefundPaymentResult>`
- `GetPaymentHandler` implements `IGetPaymentUseCase` and returns `IOperationResult<GetPaymentResult>`

### Repository Ports

- `IPaymentRepository`
- `IPaymentReadRepository`

### Controllers

- `PaymentsAdministratorController`
- `PaymentsOperatorController`

### Authorization Policies

Prefer grouped policies when capabilities are related:

- `IAdministratorPaymentAccessPolicy`
- `AdministratorPaymentAccessPolicy`

Use capability-specific policies only when the rule is complex or reused in isolation:

- `IAdministratorRefundPaymentPolicy`
- `AdministratorRefundPaymentPolicy`

Avoid generic names such as:

- `IAdministrator`
- `IOperator`
- `Service`
- `Manager`
- `Helper`

Those names get too broad too quickly and usually become architectural junk drawers.

## Default Rules for Use Cases

Each use case should be small, explicit, and role-oriented.

Use this shape:

```text
Application/
  UseCases/
    <Role>/
      Commands/
        <Action>/
          <Action>Command.cs
          <Action>Handler.cs
          <Action>Result.cs
      Queries/
        <Action>/
          <Action>Query.cs
          <Action>Handler.cs
          <Action>Result.cs
```

Default assumptions:

- one business action maps to one use case
- commands mutate state
- queries do not mutate state
- a handler orchestrates, but does not become a second domain model
- the input port interface is the boundary contract; the handler is the implementation

### Input port vs handler

- `Ports/In/.../IRefundPaymentUseCase.cs` is the inbound contract.
- `UseCases/.../RefundPaymentHandler.cs` implements that contract.

Controllers and other adapters depend on the port, not on the concrete handler type.

## Default Rules for Roles

Roles are first-class at the application boundary, but not inside the business model.

Roles should appear in:

- `Application/Ports/In/<Role>/`
- `Application/UseCases/<Role>/`
- `Application/Authorization/<Role>/`
- `Presentation/Http/<Role>/`

Roles should not appear in:

- aggregate names
- entity names
- value object names
- domain event names
- repository port names

Correct:

```text
Payments/Application/UseCases/Administrator/Commands/RefundPayment/
Payments/Presentation/Http/Operator/
```

Incorrect:

```text
Payments/Domain/Aggregates/AdministratorPayment/
Payments/Domain/OperatorPayment.cs
```

## Aggregate Rules

Aggregates are the center of consistency.

Every aggregate should:

- own its invariants
- expose behavior, not arbitrary mutation
- be the only valid mutation entry point for its consistency boundary

Minimum aggregate structure:

```text
Domain/Aggregates/<AggregateName>/
  <AggregateName>.cs
  <AggregateName>Id.cs
```

Recommended aggregate structure (when complexity justifies it):

```text
Domain/Aggregates/<AggregateName>/
  <AggregateName>.cs
  <AggregateName>Id.cs
  <AggregateName>State.cs
  Rules/
  ValueObjects/
  Events/
```

## Event Sourcing Rule Set

Event sourcing must be treated as a plug-in persistence strategy for selected aggregates.

It is not a separate architectural root.

### What stays stable

The domain remains:

- the aggregate
- its invariants
- its domain events

The application remains:

- the use cases
- the input ports
- the output ports

### What changes when event sourcing is adopted

Infrastructure changes:

- repository implementation
- event store implementation
- stream version handling
- projections
- snapshots

### The required repository port

Use a domain-level repository port like this:

```csharp
public interface I<AggregateName>Repository
{
    Task<<AggregateName>?> GetByIdAsync(<AggregateName>Id id, CancellationToken ct = default);
    Task SaveAsync(<AggregateName> aggregate, CancellationToken ct = default);
}
```

For example:

```csharp
public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(PaymentId id, CancellationToken ct = default);
    Task SaveAsync(Payment payment, CancellationToken ct = default);
}
```

The application must depend on this interface, not on `IEventStore`.

### Infrastructure implementations

State-based:

```text
Infrastructure/Persistence/StateBased/Repositories/PaymentRepository.cs
```

Event-sourced:

```text
Infrastructure/Persistence/EventSourcing/EventStore/PaymentStreamRepository.cs
```

Both implement the same repository port.

Until event sourcing is adopted for a given aggregate, a flat `Infrastructure/Persistence/Repositories/` layout is enough.

### Hard rule

If an aggregate becomes event sourced:

- events belong to the domain
- stream/versioning belongs to infrastructure
- projections belong to infrastructure
- read models belong to infrastructure
- callers must not care whether the aggregate is stored by state or by events

## Optimistic Concurrency

Concurrency control is a production concern. It is not mandatory ceremony on day one for every aggregate, but it must be designed in before conflicting writes become likely.

### Rules

- State-based aggregates: add version / etag when concurrent updates are real.
- Event-sourced aggregates: expected stream version is enforced inside the repository implementation.
- Use cases never talk to `IEventStore` for versioning.
- The repository port stays simple (`SaveAsync(aggregate)`); the aggregate carries the version the infrastructure needs.

### Example mental model

```csharp
public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(PaymentId id, CancellationToken ct = default);
    Task SaveAsync(Payment payment, CancellationToken ct = default);
    // Event-sourced implementation reads expectedVersion from the aggregate
    // and fails the save on concurrent conflict.
}
```

Conflict failures are mapped by Application into `IOperationResult` failures and by Presentation into the appropriate HTTP response (typically 409 Conflict).

## Domain Events vs Integration Events

Do not treat every event as the same thing.

### Domain events

- Facts that happened inside one bounded context
- Belong to `Domain/` (often under the aggregate or `Domain/Events/`)
- Used to keep the local model consistent or to trigger local reactions after a successful save
- Published through `IDomainEventPublisher` (or equivalent) after persistence succeeds

### Integration events

- Contracts between bounded contexts
- Belong to Application integration surface (`Application/Integration/` and/or messaging ports)
- Must not be raw domain events reused as public contracts
- Published through a dedicated integration port (for example `IIntegrationEventPublisher`)

### Hard rule

Domain events are internal. Integration events are explicit external contracts. Map deliberately from one to the other when a local fact must be shared.

## Read and Write Model Guidance

Always separate mutation concerns from read concerns, even if they remain in the same bounded context.

### Write side

Use:

- aggregate repositories
- commands
- command handlers
- aggregate behavior

### Read side

Use:

- read repositories
- queries
- query handlers
- projection-backed data when necessary

Role-specific visibility usually affects reads more than writes. That is why role-aware list/detail views should usually be implemented through query handlers and read repositories, not through role-specific aggregates.

## HTTP Contracts Rule

HTTP contracts belong to `Presentation/`.

Application contracts belong to `Application/`.

Do not use transport contracts directly as domain or use case contracts.

Correct:

```text
Presentation/Http/Administrator/Contracts/RefundPaymentRequest.cs
Application/UseCases/Administrator/Commands/RefundPayment/RefundPaymentCommand.cs
```

The controller translates one to the other.

## Authorization Anti-Ceremony

Authorization belongs in `Application/Authorization/<Role>/`.

### Default

Group related capabilities into one role/domain access policy:

```text
Application/Authorization/Administrator/
  IAdministratorPaymentAccessPolicy.cs
  AdministratorPaymentAccessPolicy.cs
```

### When to split

Create a capability-specific policy only when:

- the rule is complex enough to deserve isolation, or
- the same rule is reused across multiple unrelated use cases

Do not require `IXxxPolicy` 1:1 for every use case by default.

## Example Blueprint: Payments

### Day-one scaffold (minimum)

This is what you create first. No event sourcing folders yet.

```text
Payments/
  Domain/
    Aggregates/
      Payment/
        Payment.cs
        PaymentId.cs
    ValueObjects/
      Money.cs
    Errors/
      PaymentDomainErrors.cs

  Application/
    Ports/
      In/
        Administrator/
          Commands/
            IRefundPaymentUseCase.cs
          Queries/
            IGetPaymentUseCase.cs
        Operator/
          Queries/
            IGetPaymentUseCase.cs
            ISearchPaymentsUseCase.cs
      Out/
        Persistence/
          IPaymentRepository.cs
          IPaymentReadRepository.cs
        Identity/
          IRequestContext.cs

    UseCases/
      Administrator/
        Commands/
          RefundPayment/
            RefundPaymentCommand.cs
            RefundPaymentHandler.cs
            RefundPaymentResult.cs
        Queries/
          GetPayment/
            GetPaymentQuery.cs
            GetPaymentHandler.cs
            GetPaymentResult.cs
      Operator/
        Queries/
          SearchPayments/
            SearchPaymentsQuery.cs
            SearchPaymentsHandler.cs
            SearchPaymentsResult.cs

    Authorization/
      Administrator/
        IAdministratorPaymentAccessPolicy.cs
        AdministratorPaymentAccessPolicy.cs

  Infrastructure/
    Persistence/
      Repositories/
        PaymentRepository.cs
      ReadModels/
        PaymentReadRepository.cs

  Presentation/
    Http/
      Administrator/
        PaymentsAdministratorController.cs
        Contracts/
          RefundPaymentRequest.cs
          RefundPaymentResponse.cs
      Operator/
        PaymentsOperatorController.cs
        Contracts/
          SearchPaymentsRequest.cs
          SearchPaymentsResponse.cs

  Composition/
    PaymentsServiceCollectionExtensions.cs
```

### Later additions (only when needed)

When Payments adopts event sourcing, messaging, or cross-context publication, add only the pieces required:

```text
Payments/
  Domain/
    Aggregates/
      Payment/
        Events/
          PaymentCreated.cs
          PaymentRefunded.cs

  Application/
    Ports/
      Out/
        Messaging/
          IDomainEventPublisher.cs
          IIntegrationEventPublisher.cs
    Integration/
      PaymentRefundedIntegrationEvent.cs

  Infrastructure/
    Persistence/
      StateBased/
        Repositories/
          PaymentRepository.cs
      EventSourcing/
        EventStore/
          PaymentStreamRepository.cs
        Projections/
          PaymentDetailsProjection.cs
```

The application ports and use cases stay the same. Only infrastructure (and optional domain events / integration contracts) grow.

## Explicitly Deferred

These are production concerns, but **not part of this blueprint’s structural standard**. Adopt them per use case when the need is real; document the chosen pattern then.

| Concern | Guidance when it appears |
|---|---|
| Command idempotency | Prefer idempotency keys on critical write endpoints; enforce in Application/Infrastructure, not in Domain structure |
| Outbox / reliable messaging | Use when integration events must not be lost after commit; keep behind messaging ports |
| Integration event versioning | Version contracts explicitly; never rename silently |
| Snapshot policy for ES | Add snapshots only when stream replay cost justifies it |
| MediatR / generic buses | Optional implementation detail; not required by this blueprint |

Do not expand folder structure preemptively for these items.

## Non-Negotiable Rules

1. Do not make roles the root of the folder structure.
2. Do not create giant interfaces such as `IAdministrator` with dozens of methods.
3. Do not place HTTP contracts in domain or application.
4. Do not let controllers mutate aggregates directly.
5. Do not let aggregates depend on persistence or transport details.
6. Do not expose `IEventStore` to use cases unless there is a very rare and explicit reason.
7. Do not create role-specific aggregates for the same business concept.
8. Do not turn `Shared`, `Common`, or `Helpers` into unbounded dumping grounds.
9. Do not create empty optional folders preemptively.
10. Do not reference another domain’s `Infrastructure`.
11. Do not publish raw domain events as cross-context contracts.
12. Do not invent a second Result framework; use `IOperationResult<T>`.

## Default Decision Checklist

When creating a new domain, apply these decisions in order:

1. Identify the bounded context and choose the domain name.
2. Identify the aggregate roots and their invariants.
3. Decide which roles interact with the domain.
4. Scaffold the minimum viable domain only.
5. Create input ports by role and by use case, not by generic service bucket.
6. Create output ports for aggregate persistence, reads, and external integrations as needed.
7. Put authorization in grouped application policies; split only when justified.
8. Keep HTTP contracts in presentation and map `IOperationResult<T>` at the edge.
9. Start with state-based persistence unless event history gives real value.
10. Add optimistic concurrency when concurrent writes are a real risk.
11. If event sourcing is needed, swap the repository implementation, add projections, and keep the application contract unchanged.
12. If another domain must be involved, integrate through ports, adapters, or integration events — not infrastructure coupling.

## Final Summary

The official architectural stance is:

- this document is the default standard for new and substantially changed domains
- bounded context as the root
- hexagonal layers inside the domain
- roles explicit at the application boundary
- aggregates as the consistency center
- event sourcing as a plug-in persistence strategy for selected aggregates
- exactly two templates: minimum by default, optional additions on demand
- clear separation between domain events and integration events
- project-native `IOperationResult<T>` as the application envelope; `*Result` as success payload
- production extras (idempotency, outbox, snapshots) deferred until needed

In short:

`role` defines who uses the domain  
`aggregate` defines what must stay consistent  
`event sourcing` defines how selected aggregates persist their history  
`ports` define the boundary  
`ceremony` is earned, not assumed
