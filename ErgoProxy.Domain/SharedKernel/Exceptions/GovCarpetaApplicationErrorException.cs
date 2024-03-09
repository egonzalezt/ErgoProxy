namespace ErgoProxy.Domain.SharedKernel.Exceptions;

public class GovCarpetaApplicationErrorException : BusinessException
{
    public GovCarpetaApplicationErrorException() : base()
    {
    }

    public GovCarpetaApplicationErrorException(string message) : base($"GovCarpeta is not working {message}")
    {
    }

    public GovCarpetaApplicationErrorException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
