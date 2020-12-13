namespace NServiceBus.Core.Tests.API
{
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;

    static class TaskReturning
    {
        private static readonly List<MethodInfo> methods = typeof(IMessage).Assembly.GetTypes()
            .Where(type => !type.IsObsolete())
            .SelectMany(type => type.GetMethods(BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(method => !method.GetCustomAttributes(typeof(ObsoleteAttribute), true)
                .Any()))
            .Where(method => typeof(Task).IsAssignableFrom(method.ReturnType))
            .ToList();

#pragma warning disable IDE0001 // Simplify Names
        private static readonly List<MethodInfo> noTokenPolicy = methods
            .Where(method =>
                (!method.IsVisible() && method.IsOn(type => (type.FullName ?? type.Name).Contains("UnitOfWork"))) ||
                method.IsOn(typeof(NServiceBus.IHandleMessages<>)) ||
                method.IsOn(typeof(NServiceBus.IHandleTimeouts<>)) ||
                method.IsOn(typeof(NServiceBus.IMessageProcessingContext)) ||
                method.IsOn(typeof(NServiceBus.IPipelineContext)) ||
                method.IsOn(typeof(NServiceBus.Saga)) ||
                method.IsOn(typeof(NServiceBus.Pipeline.MessageHandler)))
            .ToList();

        private static readonly List<MethodInfo> optionalTokenPolicy = methods
            .Where(method =>
                method.IsOn(typeof(NServiceBus.Endpoint)) ||
                method.IsOn(typeof(NServiceBus.IEndpointInstance)) ||
                method.IsOn(typeof(NServiceBus.IMessageSession)) ||
                method.IsOn(typeof(NServiceBus.IStartableEndpoint)) ||
                method.IsOn(typeof(NServiceBus.IStartableEndpointWithExternallyManagedContainer)))
            .ToList();

        private static readonly List<MethodInfo> mandatoryTokenPolicy = methods
            .Where(method =>
                (!method.IsVisible() && !method.IsOn(type => (type.FullName ?? type.Name).Contains("UnitOfWork"))) ||
                method.IsOn(typeof(NServiceBus.DataBus.IDataBus)) ||
                method.IsOn(typeof(NServiceBus.Features.FeatureStartupTask)) ||
                method.IsOn(typeof(NServiceBus.Installation.INeedToInstallSomething)) ||
                method.IsOn(typeof(NServiceBus.MessageMutator.IMutateIncomingMessages)) ||
                method.IsOn(typeof(NServiceBus.MessageMutator.IMutateIncomingTransportMessages)) ||
                method.IsOn(typeof(NServiceBus.MessageMutator.IMutateOutgoingMessages)) ||
                method.IsOn(typeof(NServiceBus.MessageMutator.IMutateOutgoingTransportMessages)) ||
                method.IsOn(typeof(NServiceBus.Outbox.IOutboxStorage)) ||
                method.IsOn(typeof(NServiceBus.Outbox.OutboxTransaction)) ||
                method.IsOn(typeof(NServiceBus.Persistence.CompletableSynchronizedStorageSession)) ||
                method.IsOn(typeof(NServiceBus.Persistence.ISynchronizedStorage)) ||
                method.IsOn(typeof(NServiceBus.Persistence.ISynchronizedStorageAdapter)) ||
                method.IsOn(typeof(NServiceBus.Pipeline.Behavior<>)) ||
                method.IsOn(typeof(NServiceBus.Pipeline.ForkConnector<,>)) ||
                method.IsOn(typeof(NServiceBus.Pipeline.IBehavior<,>)) ||
                method.IsOn(typeof(NServiceBus.Pipeline.PipelineTerminator<>)) ||
                method.IsOn(typeof(NServiceBus.Pipeline.StageConnector<,>)) ||
                method.IsOn(typeof(NServiceBus.Pipeline.StageForkConnector<,,>)) ||
                method.IsOn(typeof(NServiceBus.Sagas.IHandleSagaNotFound)) ||
                method.IsOn(typeof(NServiceBus.Sagas.ISagaPersister)) ||
                method.IsOn(typeof(NServiceBus.Sagas.IFindSagas<>.Using<>)) ||
                method.IsOn(typeof(NServiceBus.Transport.ICreateQueues)) ||
                method.IsOn(typeof(NServiceBus.Transport.IDispatchMessages)) ||
                method.IsOn(typeof(NServiceBus.Transport.IManageSubscriptions)) ||
                method.IsOn(typeof(NServiceBus.Transport.IPushMessages)) ||
                method.IsOn(typeof(NServiceBus.Transport.TransportInfrastructure)) ||
                method.IsOn(typeof(NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions.ISubscriptionStorage)))
            .ToList();
#pragma warning restore IDE0001 // Simplify Names

        [TestCase(true)]
        [TestCase(false)]
        public static void AllMethodsHaveATokenPolicy(bool visible)
        {
            var violators = methods
                .Where(method => method.IsVisible() == visible)
                .Except(noTokenPolicy)
                .Except(optionalTokenPolicy)
                .Except(mandatoryTokenPolicy)
                .Prettify()
                .ToList();

            violators.Write(nameof(AllMethodsHaveATokenPolicy), visible);

            Assert.IsEmpty(violators);
        }

        [TestCase(true)]
        [TestCase(false)]
        public static void NoTokenPolicy(bool visible)
        {
            var violators = noTokenPolicy
                .Where(method => method.IsVisible() == visible && method.GetParameters().Any(param => param.ParameterType == typeof(CancellationToken)))
                .Prettify()
                .ToList();

            violators.Write(nameof(NoTokenPolicy), visible);

            Assert.IsEmpty(violators);
        }

        [TestCase(true)]
        [TestCase(false)]
        public static void OptionalTokenPolicy(bool visible)
        {
            var violators = optionalTokenPolicy
                .Where(method => method.IsVisible() == visible && !method.GetParameters().Any(param => param.ParameterType == typeof(CancellationToken) && param.IsOptional))
                .Prettify()
                .ToList();

            violators.Write(nameof(OptionalTokenPolicy), visible);

            Assert.IsEmpty(violators);
        }

        [TestCase(true)]
        [TestCase(false)]
        public static void MandatoryTokenPolicy(bool visible)
        {
            var violators = mandatoryTokenPolicy
                .Where(method => method.IsVisible() == visible &&
                    !method.GetParameters().Any(param => param.ParameterType == typeof(CancellationToken) && !param.IsOptional))
                .Prettify()
                .ToList();

            violators.Write(nameof(MandatoryTokenPolicy), visible);

            Assert.IsEmpty(violators);
        }

        [Test]
        public static void CancellationTokenNumberPolicy()
        {
            var violators = methods
                .Where(method => method.GetParameters().Count(param => param.ParameterType == typeof(CancellationToken)) > 1)
                .Prettify()
                .ToList();

            violators.Write(nameof(CancellationTokenNumberPolicy));

            Assert.IsEmpty(violators);
        }

        [Test]
        public static void CancellationTokenPositionPolicy()
        {
            var candidates = methods
                .Where(method => method.GetParameters().Any(param => param.ParameterType == typeof(CancellationToken)))
                .ToList();

            var violators = candidates
                .Where(method => method.GetParameters().Last().ParameterType != typeof(CancellationToken))
                .Prettify()
                .ToList();

            violators.Write(nameof(CancellationTokenPositionPolicy));

            Assert.IsEmpty(violators);
        }

        [Test]
        public static void CancellationTokenNamePolicy()
        {
            var violators = methods
                .Where(method => method.GetParameters().Any(param => param.ParameterType == typeof(CancellationToken) && param.Name != "cancellationToken"))
                .Prettify()
                .ToList();

            violators.Write(nameof(CancellationTokenPositionPolicy));

            Assert.IsEmpty(violators);
        }

        [Test]
        public static void FuncParameterMandatoryTokenAndPositionPolicy()
        {
            var violators = methods
                .Where(method => method.GetParameters()
                    .Where(param => typeof(Delegate).IsAssignableFrom(param.ParameterType))
                    .Select(param => param.ParameterType.GetMethod("Invoke"))
                    .Where(invoke => typeof(Task).IsAssignableFrom(invoke.ReturnType))
                    .Select(invoke => invoke.GetParameters())
                    .Any(parameters => !parameters.Any() || !typeof(CancellationToken).IsAssignableFrom(parameters.Last().ParameterType)))
                .Prettify()
                .ToList();

            violators.Write(nameof(FuncParameterMandatoryTokenAndPositionPolicy));

            Assert.IsEmpty(violators);
        }

        static bool IsObsolete(this Type type) =>
            type.GetCustomAttributes<ObsoleteAttribute>(true).Any() || (type.DeclaringType != null && type.DeclaringType.IsObsolete());

        private static bool IsOn(this MethodInfo method, Type type) =>
            method.DeclaringType == type || (method.GetCustomAttributes<ExtensionAttribute>().Any() && type.IsAssignableFrom(method.GetParameters().First().ParameterType));

        private static bool IsOn(this MethodInfo method, Func<Type, bool> predicate) =>
            predicate(method.DeclaringType) || (method.GetCustomAttributes<ExtensionAttribute>().Any() && predicate(method.GetParameters().First().ParameterType));

        static bool IsVisible(this MethodInfo method) =>
            method.DeclaringType.IsVisible && (method.IsPublic || method.IsFamily || method.IsFamilyOrAssembly);

        static IEnumerable<MethodPrinter> Prettify(this IEnumerable<MethodInfo> methods) =>
            methods
                .OrderBy(method => method.DeclaringType.Namespace)
                .ThenBy(method => method.DeclaringType.Name)
                .ThenBy(method => method.Name)
                .ThenBy(method => method.ToString())
                .Select(method => new MethodPrinter(method));

        private static void Write(this IEnumerable<MethodPrinter> violators, string testName, bool visible) =>
            Console.Error.WriteLine($"Violators of {testName} for {(visible ? "" : "in")}visible methods:{Environment.NewLine}{string.Join(Environment.NewLine, violators)}");

        private static void Write(this IEnumerable<MethodPrinter> violators, string testName) =>
            Console.Error.WriteLine($"Violators of {testName}:{Environment.NewLine}{string.Join(Environment.NewLine, violators)}");

        private class MethodPrinter
        {
            private readonly MethodInfo method;

            public MethodPrinter(MethodInfo method) => this.method = method;

            public override string ToString() => $"{this.method.DeclaringType.FullName} {{ {method} }}";
        }
    }
}
