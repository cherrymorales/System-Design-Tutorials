namespace SystemDesignTutorials.ModularMonolith.Domain.Modules.Shared;

public sealed class BusinessRuleException(string message) : Exception(message);
