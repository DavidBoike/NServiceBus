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
                .ToArray();

            foreach (var type in behaviorTypes)
            {
                for (var baseType = type.BaseType; baseType != null && baseType != typeof(object); baseType = baseType.BaseType)
                {
                    if (baseType.IsGenericType)
                    {
                        var genericType = baseType.GetGenericTypeDefinition();
                        Assert.IsFalse(genericType == typeof(Behavior<>), $"For performance reasons, built-in behavior `{type}` is not allowed to inherit from abstract class Behavior<T>. Implement IBehavior<Tin, TOut> directly, using the same context type for both TIn and TOut.");
                    }
                }
            }
        }
    }
}
