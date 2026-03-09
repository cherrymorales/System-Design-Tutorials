namespace SystemDesignTutorials.ModularMonolith.Domain.Enums;

public enum OrderStatus
{
    Draft = 1,
    Submitted = 2,
    Reserved = 3,
    ReadyForInvoicing = 4,
    Invoiced = 5,
    Completed = 6,
    Cancelled = 7,
}
