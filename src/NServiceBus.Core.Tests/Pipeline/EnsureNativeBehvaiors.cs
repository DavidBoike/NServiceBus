namespace NServiceBus.Core.Tests.Pipeline
{
    using System.Linq;
    using NServiceBus.Pipeline;
    using NUnit.Framework;

    [TestFixture]
    public class EnsureNativeBehvaiors
    {
        [Test]
        public void CoreBehaviorsMustNotUseAbstractClass()
        {
            var behaviorTypes = typeof(IBehavior).Assembly.GetTypes()
                .Where(type => typeof(IBehavior).IsAssignableFrom(type))
                .Where(type => type.IsClass && !type.IsAbstract && type.BaseType != null && type.BaseType != typeof(object))
                .ToArray();

            foreach (var type in behaviorTypes)
            {
                if (type.BaseType.IsGenericType)
                {
                    var genericType = type.BaseType.GetGenericTypeDefinition();
                    Assert.IsFalse(genericType == typeof(Behavior<>), $"For performance reasons, built-in behavior `{type}` is not allowed to inherit from abstract class Behavior<T>. Implement IBehavior<Tin, TOut> directly, using the same context type for both TIn and TOut.");
                }
            }
        }
    }
}
