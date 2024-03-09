namespace ErgoProxy.Domain.User;

using Domain.SharedKernel;

public interface IUserUseCaseSelector<T> where T : class
{
    Task<GenericResponse<object>> ExecuteAsync(T body);
}
