namespace ErgoProxy.Domain.SharedKernel;

public class GenericResponse<T>
{
    public T? Data { get; set; }
    public string Message { get; set; }
    public int StatusCode { get; set; }
}
