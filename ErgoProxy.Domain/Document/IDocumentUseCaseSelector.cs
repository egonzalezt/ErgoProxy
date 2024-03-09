namespace ErgoProxy.Domain.Document;

using Domain.SharedKernel;

public interface IDocumentUseCaseSelector<T> where T : class
{
    Task<GenericResponse<object>> ExecuteAsync(T body);
}
