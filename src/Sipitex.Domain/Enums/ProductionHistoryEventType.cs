namespace Sipitex.Domain.Enums;

// Eventos inmutables del historial de una orden
public enum ProductionHistoryEventType
{
    OrderCreated,
    StageAdded,
    StageRemoved,
    StageReordered,
    StageStarted,
    StagePaused,
    StageResumed,
    StageCompleted,
    StageSent,
    StageReceived,
    PartialInventoryIn,
    PartialWithdrawal,
    ProductionRegistered,
    Note,
    InstructorAssigned
}
