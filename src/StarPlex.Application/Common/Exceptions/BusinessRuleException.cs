namespace StarPlex.Application.Common.Exceptions;

public class BusinessRuleException : AppException
{
    public BusinessRuleException(string message) : base(message)
    {
    }
}
