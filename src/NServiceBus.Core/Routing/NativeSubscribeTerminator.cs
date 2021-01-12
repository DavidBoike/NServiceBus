namespace NServiceBus
{
    using System.Threading;
    using System.Threading.Tasks;
    using Pipeline;
    using Transport;

    class NativeSubscribeTerminator : PipelineTerminator<ISubscribeContext>
    {
        public NativeSubscribeTerminator(IManageSubscriptions subscriptionManager)
        {
            this.subscriptionManager = subscriptionManager;
        }

        protected override Task Terminate(ISubscribeContext context, CancellationToken token)
        {
            return subscriptionManager.Subscribe(context.EventType, context.Extensions);
        }

        readonly IManageSubscriptions subscriptionManager;
    }
}