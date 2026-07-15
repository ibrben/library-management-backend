namespace LibraryManagement.Business.Exceptions;

public sealed class UnauthorizedException(string message) : AppException(message);
