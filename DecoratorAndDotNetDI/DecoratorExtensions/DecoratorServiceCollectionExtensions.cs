using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for adding decorators over services to 
    /// an <see cref="IServiceCollection"/>.
    /// </summary>
    public static class DecoratorServiceCollectionExtensions
    {
        /// <summary>
        /// Add a decorator over a service implementation to augment its functionality.
        /// </summary>
        /// <typeparam name="TService"> The type of the service to add.</typeparam>
        /// <typeparam name="TDecorator">The type of the decorator implementation.</typeparam>
        /// <param name="serviceCollection">
        /// The <see cref="IServiceCollection"/> to add the service decorator to.</param>
        /// <param name="configureDecorateeServices">
        /// The callback used to configure the inner (decorated) 
        /// service and its dependencies.
        /// </param>
        /// <remarks>
        /// This extension method allows decorators nesting, so you can augment your 
        /// service in multiple levels. Also this method respects lifetime scopes, 
        /// creating new instances using the same rules as when using usual service registration.
        /// If a decorator or/and a decoratee implements <see cref="IDisposable"/>, 
        /// the <see cref="IDisposable.Dispose"/> will be called at the end of lifetime scope.
        /// </remarks>
        public static void AddDecorator<TService, TDecorator>(
            this IServiceCollection serviceCollection,
            Action<IServiceCollection> configureDecorateeServices)
            where TDecorator : class, TService
            where TService : class
        {
            if (serviceCollection == null)
                throw new ArgumentNullException(nameof(serviceCollection));

            if (configureDecorateeServices == null)
                throw new ArgumentNullException(nameof(configureDecorateeServices));

            var decorateeServices = new ServiceCollection();

            // This calls back to the decoratee configuration lambda.
            configureDecorateeServices(decorateeServices);

            var decorateeDescriptor =
                // For now, support defining only single decoratee.
                // TODO: To support cases such as composite decorators 
                // (accepting multiple decoratees and representing them as single)
                // implement handling multiple decoratee configurations.
                decorateeServices.SingleOrDefault(sd => sd.ServiceType == typeof(TService));

            if (decorateeDescriptor == null)
            {
                throw new InvalidOperationException("No decoratee configured!");
            }

            // We will replace this descriptor with a tweaked one later.
            decorateeServices.Remove(decorateeDescriptor);

            // Add all remaining services to main collection.
            serviceCollection.Add(decorateeServices);

            // This factory allows us to pass some dependencies 
            // (the decoratee instance) manually,
            // which is not possible with something like GetRequiredService. 
            var decoratorInstanceFactory = ActivatorUtilities.CreateFactory(
                typeof(TDecorator), new[] { typeof(TService) });

            Type decorateeImplType = decorateeDescriptor.GetImplementationType();

            // It's important for this delegate instance to have concrete return
            // type (not just object) so when using nested decorators
            // the GetImplementationType method can correctly determine
            // decorator implementation type from delegate type parameter.
            // If this is refactored to a local function without manually 
            // wrapping in a delegate, the automatic wrapping will produce
            // return type of object, which will work incorrectly.
            Func<IServiceProvider, TDecorator> decoratorFactory = sp =>
            {
                // Note that we query the decoratee by it's implementation type,
                // avoiding any ambiguity. 
                var decoratee = sp.GetRequiredService(decorateeImplType);
                // Pass the decoratee manually. All other dependencies are resolved as usual.
                var decorator = (TDecorator)decoratorInstanceFactory(sp, new[] { decoratee });
                return decorator;
            };

            // Decorator inherits decoratee's lifetime.
            var decoratorDescriptor = ServiceDescriptor.Describe(
                typeof(TService),
                decoratorFactory,
                decorateeDescriptor.Lifetime);

            // Re-create the decoratee without original service type (interface).
            // This allows to create decoratee instances via
            // service provider, utilizing its lifetime scope
            // control functionality.
            decorateeDescriptor = RefactorDecorateeDescriptor(decorateeDescriptor);

            serviceCollection.Add(decorateeDescriptor);
            serviceCollection.Add(decoratorDescriptor);
        }

        /// <summary>
        /// The goal of this method is to replace the service type (interface)
        /// with the implementation type in any kind of service descriptor.
        /// Actually, we build new service descriptor.
        /// </summary>
        private static ServiceDescriptor RefactorDecorateeDescriptor(ServiceDescriptor decorateeDescriptor)
        {
            var decorateeImplType = decorateeDescriptor.GetImplementationType();

            if (decorateeDescriptor.ImplementationFactory != null)
            {
                decorateeDescriptor =
                    ServiceDescriptor.Describe(
                    serviceType: decorateeImplType,
                    decorateeDescriptor.ImplementationFactory,
                    decorateeDescriptor.Lifetime);
            }
            else
            if (decorateeDescriptor.ImplementationInstance != null)
            {
                decorateeDescriptor =
                    ServiceDescriptor.Singleton(
                    serviceType: decorateeImplType,
                    decorateeDescriptor.ImplementationInstance);
            }
            else
            {
                decorateeDescriptor =
                    ServiceDescriptor.Describe(
                    decorateeImplType, // Yes, use the same type for both.
                    decorateeImplType,
                    decorateeDescriptor.Lifetime);
            }

            return decorateeDescriptor;
        }

        /// <summary>
        /// Infers the implementation type for any kind of service descriptor
        /// (i.e. even when implementation type is not specified explicitly).
        /// </summary>
        private static Type GetImplementationType(this ServiceDescriptor serviceDescriptor)
        {
            if (serviceDescriptor.ImplementationType != null)
                return serviceDescriptor.ImplementationType;

            if (serviceDescriptor.ImplementationInstance != null)
                return serviceDescriptor.ImplementationInstance.GetType();

            // Get the type from the return type of the factory delegate.
            // Due to covariance, the delegate object can have more concrete type
            // than the factory delegate defines (object).
            if (serviceDescriptor.ImplementationFactory != null)
                return serviceDescriptor.ImplementationFactory.GetType().GenericTypeArguments[1];

            // This should not be possible, but just in case.
            throw new InvalidOperationException("No way to get the decoratee implementation type.");
        }
    }
}
