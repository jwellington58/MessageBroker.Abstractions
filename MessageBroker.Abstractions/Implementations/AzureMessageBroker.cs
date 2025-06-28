using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OperationResult;
using System.Text;

namespace MessageBroker.Abstractions
{
    public sealed class AzureMessageBroker : IMessageBroker
    {
        private readonly MessageBrokerOptions _options;
        private readonly ServiceBusClient _client;
        private readonly Dictionary<string, Lazy<ServiceBusSender>> _senders;
        private readonly Dictionary<string, Lazy<ServiceBusProcessor>> _processors;
        

        public AzureMessageBroker(IOptions<MessageBrokerOptions> options)
        {
            _options = options.Value;
            _client = new ServiceBusClient(options.Value.ConnectionString);
            _processors = CreateProcessors(options.Value);
            _senders = CreateSenders(options.Value);
        }

        public Task Publish<T>(T message)
            where T : class
            => GetSender<T>().SendMessageAsync(new ServiceBusMessage(JsonConvert.SerializeObject(message)));

        public Task Publish<T>(T message, string queueName)
            => CreateSender(queueName).Value.SendMessageAsync(new ServiceBusMessage(JsonConvert.SerializeObject(message)));

        public Task PublishSchedule<T>(T message, DateTime scheduledEnqueueTime)
          where T : class
          => GetSender<T>().ScheduleMessageAsync(new ServiceBusMessage(JsonConvert.SerializeObject(message)), scheduledEnqueueTime);

        public Task Publish<T>(IEnumerable<T> messages)
            where T : class
            => GetSender<T>().SendMessagesAsync(messages.Select(m => new ServiceBusMessage(JsonConvert.SerializeObject(m))));

        public Task PublishSchedule<T>(IEnumerable<T> messages, DateTime scheduledEnqueueTime)
            where T : class
            => GetSender<T>().ScheduleMessagesAsync(messages.Select(m => new ServiceBusMessage(JsonConvert.SerializeObject(m))), scheduledEnqueueTime);

        public Task RegisterEventHandler<T>(Func<T, CancellationToken, Task<Result>> callBack)
            where T : class
        {
            var processor = GetProcessor<T>();

            processor.ProcessMessageAsync += async (args) =>
            {
                try
                {
                    var result = await callBack(JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(args.Message.Body.ToArray()), new JsonSerializerSettings { ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor }), args.CancellationToken);
                    if (result.IsSuccess)
                        await args.CompleteMessageAsync(args.Message);
                    else
                        await args.AbandonMessageAsync(args.Message);
                }
                catch (JsonSerializationException)
                {
                    await args.AbandonMessageAsync(args.Message);
                }
            };

            processor.ProcessErrorAsync += Processor_ProcessErrorAsync;

            return processor.StartProcessingAsync();
        }

        private Task Processor_ProcessErrorAsync(ProcessErrorEventArgs arg)
        {
              return Task.CompletedTask;
        }

        private ServiceBusSender GetSender<T>()
            => _senders[_options[typeof(T).Name]].Value;

        private ServiceBusProcessor GetProcessor<T>()
            => _processors[_options[typeof(T).Name]].Value;

        private Dictionary<string, Lazy<ServiceBusProcessor>> CreateProcessors(MessageBrokerOptions options)
        {
            var dict = new Dictionary<string, Lazy<ServiceBusProcessor>>();
            foreach (var queue in options.QueuesByEvent)
                dict[queue.Value.QueueName] = new Lazy<ServiceBusProcessor>(() => _client.CreateProcessor(queue.Value.QueueName, new ServiceBusProcessorOptions
                {
                    AutoCompleteMessages = false,
                    ReceiveMode = ServiceBusReceiveMode.PeekLock,
                    MaxAutoLockRenewalDuration = TimeSpan.FromMinutes(queue.Value.MaxAutoLockRenewalDurationInMinutes),
                    PrefetchCount = queue.Value.PrefetchCount,
                    MaxConcurrentCalls = queue.Value.MaxConcurrentCalls
                }));
            return dict;
        }

        private Dictionary<string, Lazy<ServiceBusSender>> CreateSenders(MessageBrokerOptions options)
        {
            var dict = new Dictionary<string, Lazy<ServiceBusSender>>();
            foreach (var queue in options.QueuesByEvent)
                dict[queue.Value.QueueName] = new Lazy<ServiceBusSender>(() => _client.CreateSender(queue.Value.QueueName));
            return dict;
        }

        private Lazy<ServiceBusSender> CreateSender(string queueName)
            => new Lazy<ServiceBusSender>(() => _client.CreateSender(queueName));
    }

}
