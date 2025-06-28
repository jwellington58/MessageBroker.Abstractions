using OperationResult;

namespace MessageBroker.Abstractions
{
    public interface IMessageBroker
    {
        Task Publish<T>(T message) where T : class;
        Task Publish<T>(T message, string queueName);
        Task PublishSchedule<T>(T message, DateTime scheduledEnqueueTime) where T : class;
        Task Publish<T>(IEnumerable<T> messages) where T : class;
        Task PublishSchedule<T>(IEnumerable<T> messages, DateTime scheduledEnqueueTime) where T : class;
        Task RegisterEventHandler<T>(Func<T, CancellationToken, Task<Result>> callBack) where T : class;
    }
}
