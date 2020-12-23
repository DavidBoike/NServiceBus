namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    class MessageSession : IMessageSession
    {
        public MessageSession(RootContext context)
        {
            this.context = context;
            messageOperations = context.Get<MessageOperations>();
        }

        public Task Send(object message, SendOptions sendOptions)
        {
            return Send(message, sendOptions, CancellationToken.None);
        }

        public Task Send(object message, SendOptions sendOptions, CancellationToken cancellationToken)
        {
            Guard.AgainstNull(nameof(message), message);
            Guard.AgainstNull(nameof(sendOptions), sendOptions);
            Guard.AgainstNull(nameof(cancellationToken), cancellationToken);
            return messageOperations.Send(context, message, sendOptions, cancellationToken);
        }

        public Task Send<T>(Action<T> messageConstructor, SendOptions sendOptions)
        {
            Guard.AgainstNull(nameof(messageConstructor), messageConstructor);
            Guard.AgainstNull(nameof(sendOptions), sendOptions);
            return messageOperations.Send(context, messageConstructor, sendOptions);
        }

        public Task Publish(object message, PublishOptions publishOptions)
        {
            Guard.AgainstNull(nameof(message), message);
            Guard.AgainstNull(nameof(publishOptions), publishOptions);
            return messageOperations.Publish(context, message, publishOptions);
        }

        public Task Publish<T>(Action<T> messageConstructor, PublishOptions publishOptions)
        {
            Guard.AgainstNull(nameof(messageConstructor), messageConstructor);
            Guard.AgainstNull(nameof(publishOptions), publishOptions);
            return messageOperations.Publish(context, messageConstructor, publishOptions);
        }

        public Task Subscribe(Type eventType, SubscribeOptions subscribeOptions)
        {
            Guard.AgainstNull(nameof(eventType), eventType);
            Guard.AgainstNull(nameof(subscribeOptions), subscribeOptions);
            return messageOperations.Subscribe(context, eventType, subscribeOptions);
        }

        public Task Unsubscribe(Type eventType, UnsubscribeOptions unsubscribeOptions)
        {
            Guard.AgainstNull(nameof(eventType), eventType);
            Guard.AgainstNull(nameof(unsubscribeOptions), unsubscribeOptions);
            return messageOperations.Unsubscribe(context, eventType, unsubscribeOptions);
        }

        RootContext context;
        MessageOperations messageOperations;
    }
}