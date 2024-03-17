namespace InDuckTor.Shared.Models;

public class ForbiddenException(string? message) : ApplicationException(message);