namespace LibraryManagement.Business.Exceptions;

public sealed class ValidationException(string message, IReadOnlyDictionary<string, string[]> errors)
    : AppException(message)
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}
