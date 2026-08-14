using Refactor.Nexus.Api.Operations.Application.UseCases.Administrator.Commands;
using Refactor.Nexus.Api.Operations.Application.UseCases.Administrator.Queries;
using Refactor.Nexus.Api.Operations.Domain.Aggregates.Operation;
using Refactor.Nexus.Api.Operations.Domain.Aggregates.Store;
using Refactor.Nexus.Api.Operations.Domain.Errors;
using Refactor.Nexus.Api.Tests.Fakes;

namespace Refactor.Nexus.Api.Tests;

public sealed class OperationUseCaseTests
{
    [Fact]
    public async Task Create_save_reload_is_draft_with_key()
    {
        var fixture = Fixture.Build();
        var created = await fixture.Create.HandleAsync(new CreateOperationCommand("Front A", 5));
        Assert.True(created.IsSuccess);

        var loaded = await fixture.Ops.GetByIdAsync(new OperationId(Guid.Parse(created.Value!.OperationId)));
        Assert.NotNull(loaded);
        Assert.Equal(OperationStatus.Draft, loaded!.Status);
        Assert.Equal("Front A", loaded.Name);
        Assert.Equal(5m, loaded.ManagementCutPercent);
        Assert.StartsWith("op_", loaded.Key.Value);
        Assert.Empty(loaded.UncommittedEvents);
    }

    [Fact]
    public async Task Transition_to_closed_clears_assignments_on_reload()
    {
        var fixture = Fixture.Build();
        var created = await fixture.Create.HandleAsync(new CreateOperationCommand("Front", null));
        var id = created.Value!.OperationId;

        Assert.True((await fixture.Transition.HandleAsync(new TransitionOperationCommand(id, "Active"))).IsSuccess);
        Assert.True((await fixture.Assign.HandleAsync(new AssignOperatorCommand(id, fixture.OperatorId.ToString()))).IsSuccess);
        Assert.True((await fixture.Ops.IsMemberAssignedAsync(new OperationId(Guid.Parse(id)), fixture.OperatorId)));

        Assert.True((await fixture.Transition.HandleAsync(new TransitionOperationCommand(id, "Closed"))).IsSuccess);

        var loaded = await fixture.Ops.GetByIdAsync(new OperationId(Guid.Parse(id)));
        Assert.Equal(OperationStatus.Closed, loaded!.Status);
        Assert.Empty(loaded.AssignedOperatorIds);
        Assert.False(await fixture.Ops.IsMemberAssignedAsync(new OperationId(Guid.Parse(id)), fixture.OperatorId));
    }

    [Fact]
    public async Task Closed_cannot_be_reopened_via_handler()
    {
        var fixture = Fixture.Build();
        var created = await fixture.Create.HandleAsync(new CreateOperationCommand("Front", null));
        var id = created.Value!.OperationId;
        await fixture.Transition.HandleAsync(new TransitionOperationCommand(id, "Closed"));

        var reopen = await fixture.Transition.HandleAsync(new TransitionOperationCommand(id, "Active"));
        Assert.True(reopen.IsFailure);
        Assert.Equal(OperationErrorCodes.AlreadyClosed, reopen.Errors.First().Code);
    }

    [Fact]
    public async Task Assign_rejects_ineligible_operator()
    {
        var fixture = Fixture.Build();
        var created = await fixture.Create.HandleAsync(new CreateOperationCommand("Front", null));
        var id = created.Value!.OperationId;
        await fixture.Transition.HandleAsync(new TransitionOperationCommand(id, "Active"));

        var result = await fixture.Assign.HandleAsync(new AssignOperatorCommand(id, Guid.NewGuid().ToString()));
        Assert.True(result.IsFailure);
        Assert.Equal(OperationErrorCodes.OperatorNotEligible, result.Errors.First().Code);
    }

    [Fact]
    public async Task Cut_over_100_fails_without_persisting()
    {
        var fixture = Fixture.Build();
        var created = await fixture.Create.HandleAsync(new CreateOperationCommand("Front", null));
        var id = created.Value!.OperationId;

        var result = await fixture.Cut.HandleAsync(new ConfigureManagementCutCommand(id, 101));
        Assert.True(result.IsFailure);
        Assert.Equal(OperationErrorCodes.CutInvalid, result.Errors.First().Code);

        var loaded = await fixture.Ops.GetByIdAsync(new OperationId(Guid.Parse(id)));
        Assert.Null(loaded!.ManagementCutPercent);
    }

    [Fact]
    public async Task List_store_returns_saved_object_without_type_filter()
    {
        var fixture = Fixture.Build();
        var created = await fixture.Create.HandleAsync(new CreateOperationCommand("Front Store", null));
        var operationId = created.Value!.OperationId;
        var operation = await fixture.Ops.GetByIdAsync(new OperationId(Guid.Parse(operationId)));
        var stored = StoreObject.Create(operation!.Key, "note", """{"ok":true}""").Value!;
        await fixture.Store.SaveAsync(stored);

        var listed = await fixture.ListStore.HandleAsync(new ListStoreObjectsQuery(operationId, null));

        Assert.True(listed.IsSuccess);
        var item = Assert.Single(listed.Value!.Items);
        Assert.Equal("note", item.ObjectType);
        Assert.Contains("ok", item.PayloadJson);
    }

    private sealed class Fixture
    {
        public Guid AdminId { get; } = Guid.NewGuid();
        public Guid OperatorId { get; } = Guid.NewGuid();
        public InMemoryOperationRepository Ops { get; } = new();
        public InMemoryStoreObjectRepository Store { get; } = new();
        public CreateOperationHandler Create { get; }
        public TransitionOperationHandler Transition { get; }
        public AssignOperatorHandler Assign { get; }
        public ConfigureManagementCutHandler Cut { get; }
        public ListStoreObjectsHandler ListStore { get; }

        private Fixture()
        {
            var context = new AdminRequestContext(AdminId);
            var gate = new AdminCapabilityGate(AdminId);
            Create = new CreateOperationHandler(context, gate, Ops);
            Transition = new TransitionOperationHandler(context, gate, Ops);
            Assign = new AssignOperatorHandler(context, gate, new FixedOperatorEligibility(OperatorId), Ops);
            Cut = new ConfigureManagementCutHandler(context, gate, Ops);
            ListStore = new ListStoreObjectsHandler(context, gate, Ops, Store);
        }

        public static Fixture Build() => new();
    }
}
