namespace LibraryManagement.Business.Exceptions;

public sealed class ForbiddenException(string message) : AppException(message);
