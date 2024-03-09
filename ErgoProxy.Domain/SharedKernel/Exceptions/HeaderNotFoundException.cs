namespace ErgoProxy.Domain.SharedKernel.Exceptions;

public class HeaderNotFoundException : BusinessException
{
    public HeaderNotFoundException() : base()
    {
    }

    public HeaderNotFoundException(string message) : base($"Header {message} not found")
    {
    }

    public HeaderNotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
