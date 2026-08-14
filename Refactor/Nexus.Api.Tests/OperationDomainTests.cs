using Refactor.Nexus.Api.Operations.Domain.Aggregates.Operation;
using Refactor.Nexus.Api.Operations.Domain.Errors;
using Refactor.Nexus.Api.Operations.Domain.Events;
using OperationAggregate = Refactor.Nexus.Api.Operations.Domain.Aggregates.Operation.Operation;

namespace Refactor.Nexus.Api.Tests;

public sealed class OperationDomainTests
{
    [Fact]
    public void Create_mints_unique_key_in_draft()
    {
        var created = OperationAggregate.Create("Front A");
        Assert.True(created.IsSuccess);
        Assert.Equal(OperationStatus.Draft, created.Value!.Status);
        Assert.StartsWith("op_", created.Value.Key.Value);
    }

    [Fact]
    public void Closed_cannot_reopen()
    {
        var op = OperationAggregate.Create("Front").Value!;
        Assert.True(op.TransitionTo(OperationStatus.Closed).IsSuccess);
        var reopen = op.TransitionTo(OperationStatus.Active);
        Assert.True(reopen.IsFailure);
        Assert.Equal(OperationErrorCodes.AlreadyClosed, reopen.Errors.First().Code);
    }

    [Fact]
    public void Invalid_transition_fails()
    {
        var op = OperationAggregate.Create("Front").Value!;
        var result = op.TransitionTo(OperationStatus.Paused);
        Assert.True(result.IsFailure);
        Assert.Equal(OperationErrorCodes.InvalidTransition, result.Errors.First().Code);
    }

    [Fact]
    public void Close_clears_assignments()
    {
        var op = OperationAggregate.Create("Front").Value!;
        Assert.True(op.TransitionTo(OperationStatus.Active).IsSuccess);
        var member = Guid.NewGuid();
        Assert.True(op.AssignOperator(member).IsSuccess);
        Assert.True(op.TransitionTo(OperationStatus.Closed).IsSuccess);
        Assert.Empty(op.AssignedOperatorIds);
    }

    [Fact]
    public void Closed_cannot_assign()
    {
        var op = OperationAggregate.Create("Front").Value!;
        Assert.True(op.TransitionTo(OperationStatus.Closed).IsSuccess);
        var result = op.AssignOperator(Guid.NewGuid());
        Assert.True(result.IsFailure);
        Assert.Equal(OperationErrorCodes.AlreadyClosed, result.Errors.First().Code);
        Assert.DoesNotContain(op.UncommittedEvents, e => e is OperatorAssigned);
    }

    [Fact]
    public void Duplicate_assign_fails_without_event()
    {
        var op = OperationAggregate.Create("Front").Value!;
        Assert.True(op.TransitionTo(OperationStatus.Active).IsSuccess);
        var member = Guid.NewGuid();
        Assert.True(op.AssignOperator(member).IsSuccess);
        var count = op.UncommittedEvents.Count;
        var again = op.AssignOperator(member);
        Assert.True(again.IsFailure);
        Assert.Equal(OperationErrorCodes.AlreadyAssigned, again.Errors.First().Code);
        Assert.Equal(count, op.UncommittedEvents.Count);
    }

    [Fact]
    public void Unassign_missing_member_fails()
    {
        var op = OperationAggregate.Create("Front").Value!;
        var result = op.UnassignOperator(Guid.NewGuid());
        Assert.True(result.IsFailure);
        Assert.Equal(OperationErrorCodes.NotAssigned, result.Errors.First().Code);
    }

    [Fact]
    public void Draft_active_draft_is_allowed()
    {
        var op = OperationAggregate.Create("Front").Value!;
        Assert.True(op.TransitionTo(OperationStatus.Active).IsSuccess);
        Assert.True(op.TransitionTo(OperationStatus.Draft).IsSuccess);
        Assert.Equal(OperationStatus.Draft, op.Status);
    }

    [Fact]
    public void Invalid_cut_does_not_append()
    {
        var op = OperationAggregate.Create("Front").Value!;
        var count = op.UncommittedEvents.Count;
        var result = op.ConfigureManagementCut(101);
        Assert.True(result.IsFailure);
        Assert.Equal(OperationErrorCodes.CutInvalid, result.Errors.First().Code);
        Assert.Equal(count, op.UncommittedEvents.Count);
    }

    [Fact]
    public void Paused_blocks_script_and_store_write()
    {
        var op = OperationAggregate.Create("Front").Value!;
        Assert.True(op.TransitionTo(OperationStatus.Active).IsSuccess);
        Assert.True(op.TransitionTo(OperationStatus.Paused).IsSuccess);
        Assert.False(op.AllowsScriptResolve);
        Assert.False(op.AllowsStoreWrite);
        Assert.False(op.AllowsNewCharging);
    }
}
