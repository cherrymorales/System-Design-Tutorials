namespace SystemDesignTutorials.ClientServerSpaApi.Domain;

public sealed class BusinessRuleException(string message) : Exception(message);
