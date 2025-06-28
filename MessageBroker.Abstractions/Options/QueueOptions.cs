namespace MessageBroker.Abstractions
{
    public class QueueOptions
    {
        public string QueueName { get; set; }
        public int MaxAutoLockRenewalDurationInMinutes { get; set; } = 5;
        public int PrefetchCount { get; set; } = 1;
        public int MaxConcurrentCalls { get; set; } = 1;
    }
}
