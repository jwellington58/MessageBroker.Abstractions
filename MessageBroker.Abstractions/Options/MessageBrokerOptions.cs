namespace MessageBroker.Abstractions
{
    public sealed class MessageBrokerOptions
    {
        public string ConnectionString { get; set; }
        public Dictionary<string, QueueOptions> QueuesByEvent { get; set; }
        public string this[string @event] => QueuesByEvent[@event].QueueName;
    }
}
