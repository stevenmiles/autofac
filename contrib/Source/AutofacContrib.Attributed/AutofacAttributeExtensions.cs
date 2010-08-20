﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;
using Autofac;
using Autofac.Builder;
using Autofac.Features.Metadata;
using Autofac.Features.Scanning;

namespace AutofacContrib.Attributed
{
    public static class AutofacAttributeExtensions
    {
        #region discovery suppport methods

        /// <summary>
        /// a shortcircuit method of looking at attributes to see if they are, in turn, attributed with a Metadata attribute
        /// </summary>
        /// <param name="typeInfo">type under investigation</param>
        /// <returns>true if any metadata attributed attributes are found</returns>
        private static bool HasMetadataAttribute(Type typeInfo)
        {
            return typeInfo.GetCustomAttributes(true).Cast<Attribute>().Any(info => info.GetType().GetCustomAttributes(typeof(MetadataAttributeAttribute), false).Count() > 0);
        }


        /// <summary>
        /// retrieves a dictionary of public properties on an attribute
        /// </summary>
        /// <param name="attribute">attribute being queried</param>
        /// <returns>dictionary of property names and their values</returns>
        private static IDictionary<string, object> GetProperties(Attribute attribute)
        {
            return attribute.GetType().GetProperties().Where(propertyInfo => propertyInfo.CanRead &&
                                                                      propertyInfo.DeclaringType.Name !=
                                                                      typeof(Attribute).Name)
                .Select(propertyInfo => new KeyValuePair<string, object>
                                            (propertyInfo.Name, propertyInfo.GetValue(attribute, null))).ToDictionary(pair => pair.Key, pair => pair.Value);


        }

        /// <summary>
        /// retrieves the strongly typed metadata instances associated with a given target type
        /// </summary>
        /// <typeparam name="TMetadata">metadata type</typeparam>
        /// <param name="targetType">instance being interrogated for metadata</param>
        /// <returns>enumerable set of metadata associated with the target type</returns>
        private static IEnumerable<TMetadata> GetStronglyTypedMetadata<TMetadata>(Type targetType)
        {
            return from Attribute attribute in targetType.GetCustomAttributes(true) where attribute.GetType().GetCustomAttributes(typeof(MetadataAttributeAttribute), false).Count() > 0 select AttributedModelServices.GetMetadataView<TMetadata>(GetProperties(attribute));
        }


        private static IEnumerable<IDictionary<string, object>> GetMetadata(Type targetType)
        {
            return from Attribute attribute in targetType.GetCustomAttributes(true)
                   where attribute.GetType().GetCustomAttributes(typeof(MetadataAttributeAttribute), false).Count() > 0
                   select GetProperties(attribute);
        }


        #endregion



        public static IRegistrationBuilder<TLimit, TScanningActivatorData, TRegistrationStyle> WithAttributedMetadata<TLimit, TScanningActivatorData, TRegistrationStyle>
                        (this IRegistrationBuilder<TLimit, TScanningActivatorData, TRegistrationStyle> registration)
                                        where TScanningActivatorData : ScanningActivatorData
        {
            // Count required otherwise the lazyness of the expression is one degree too lazy
            registration.ActivatorData.ConfigurationActions.Add(
                (t, rb) => GetMetadata(t).Select(rb.WithMetadata).Count());

            return registration;
        }



        public static void RegisterAssemblyTypedMetadata<TInterface, TMetadata>(this ContainerBuilder builder, params Assembly[] assemblies)
        {
            builder.RegisterAssemblyTypedMetadata<TInterface, TMetadata>(p => true, assemblies);
        }


        public static void RegisterAssemblyTypedMetadata<TInterface, TMetadata>(this ContainerBuilder builder, Predicate<TMetadata> inclusionPredicate, params Assembly[] assemblies)
        {
            if (inclusionPredicate == null)
                throw new ArgumentNullException("inclusionPredicate");

            foreach (var targetType in assemblies.Select(assembly => (from type in assembly.GetTypes()
                                                                      where type.IsClass && type.GetInterface(typeof(TInterface).Name) != null && HasMetadataAttribute(type)
                                                                      select type)).SelectMany(targetTypes => targetTypes))
                builder.RegisterTypedMetadata<TInterface, TMetadata>(targetType,
                                                                     GetStronglyTypedMetadata<TMetadata>(targetType).
                                                                         Where(a => inclusionPredicate(a)));
        }

        public static IRegistrationBuilder<TInstance, ConcreteReflectionActivatorData, SingleRegistrationStyle> RegisterTypedMetadata<TInterface, TInstance, TMetadata>(this ContainerBuilder builder, IEnumerable<TMetadata> metadataSet) where TInstance : TInterface
        {
            foreach (var metadata in metadataSet)
            {
                var localMetadata = metadata;

                // register Lazy<T, TMetadata> type
                builder.Register(c => new Lazy<TInterface, TMetadata>(() => c.Resolve<TInstance>(), localMetadata));

                // register the Meta<T, TMetadata> type
                builder.Register(c => new Meta<TInterface, TMetadata>(c.Resolve<TInstance>(), localMetadata));
            }

            return builder.RegisterType<TInstance>();

        }

        public static IRegistrationBuilder<object, ConcreteReflectionActivatorData, SingleRegistrationStyle> RegisterTypedMetadata<TInterface, TMetadata>(this ContainerBuilder builder, Type instanceType, IEnumerable<TMetadata> metadataSet)
        {
            foreach (var metadata in metadataSet)
            {
                var localMetadata = metadata;

                // register Lazy<T, TMetadata> type
                builder.Register(c => new Lazy<TInterface, TMetadata>(() => (TInterface)c.Resolve(instanceType), localMetadata));

                // register the Meta<T, TMetadata> type
                builder.Register(c => new Meta<TInterface, TMetadata>((TInterface)c.Resolve(instanceType), localMetadata));
            }

            return builder.RegisterType(instanceType);
        }
    }
}
