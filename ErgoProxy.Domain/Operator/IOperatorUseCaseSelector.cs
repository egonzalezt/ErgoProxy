namespace ErgoProxy.Domain.Operator;

using Domain.SharedKernel;

public interface IOperatorUseCaseSelector<T> where T : class
{
    Task<GenericResponse<object>> ExecuteAsync(T body);
}
