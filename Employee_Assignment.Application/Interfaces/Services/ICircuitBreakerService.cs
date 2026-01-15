namespace Employee_Assignment.Application.Interfaces.Services
{
    public interface ICircuitBreakerService
    {
        Task<T> ExecuteAsync<T>(Func<Task<T>> operation);

        string GetCircuitBreakerState();
    }
}