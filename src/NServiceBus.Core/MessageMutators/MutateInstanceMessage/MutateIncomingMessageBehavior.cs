﻿namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using MessageMutator;
    using Microsoft.Extensions.DependencyInjection;
    using Pipeline;

    class MutateIncomingMessageBehavior : IBehavior<IIncomingLogicalMessageContext, IIncomingLogicalMessageContext>
    {
        public MutateIncomingMessageBehavior(HashSet<IMutateIncomingMessages> mutators)
        {
            this.mutators = mutators;
        }

        public Task Invoke(IIncomingLogicalMessageContext context, CancellationToken cancellationToken, Func<IIncomingLogicalMessageContext, CancellationToken, Task> next)
        {
            if (hasIncomingMessageMutators)
            {
                return InvokeIncomingMessageMutators(context, cancellationToken, next);
            }

            return next(context, cancellationToken);
        }

        async Task InvokeIncomingMessageMutators(IIncomingLogicalMessageContext context, CancellationToken cancellationToken, Func<IIncomingLogicalMessageContext, CancellationToken, Task> next)
        {
            var logicalMessage = context.Message;
            var current = logicalMessage.Instance;

            var mutatorContext = new MutateIncomingMessageContext(current, context.Headers);

            var hasMutators = false;

            foreach (var mutator in context.Builder.GetServices<IMutateIncomingMessages>())
            {
                hasMutators = true;

                //TODO: Decide how to pass token to mutators
                await mutator.MutateIncoming(mutatorContext)
                    .ThrowIfNull()
                    .ConfigureAwait(false);
            }

            foreach (var mutator in mutators)
            {
                hasMutators = true;

                await mutator.MutateIncoming(mutatorContext)
                    .ThrowIfNull()
                    .ConfigureAwait(false);
            }

            hasIncomingMessageMutators = hasMutators;

            if (mutatorContext.MessageInstanceChanged)
            {
                context.UpdateMessageInstance(mutatorContext.Message);
            }

            await next(context, cancellationToken).ConfigureAwait(false);
        }

        volatile bool hasIncomingMessageMutators = true;
        HashSet<IMutateIncomingMessages> mutators;
    }
}