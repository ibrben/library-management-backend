namespace LibraryManagement.Business.Exceptions;

public sealed class ConflictException(string message) : AppException(message);
