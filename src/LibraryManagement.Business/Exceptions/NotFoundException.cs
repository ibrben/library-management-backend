namespace LibraryManagement.Business.Exceptions;

public sealed class NotFoundException(string message) : AppException(message);
