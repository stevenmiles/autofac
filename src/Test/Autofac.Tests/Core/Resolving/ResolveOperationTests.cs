﻿using NUnit.Framework;
using Autofac.Core;
using Autofac.Tests.Scenarios.Dependencies;

namespace Autofac.Tests.Core.Resolving
{
    [TestFixture]
    public class ResolveOperationTests
    {
        [Test]
        public void CtorPropDependencyOkOrder1()
        {
            var cb = new ContainerBuilder();
            cb.RegisterType<DependsByCtor>().SingleInstance();
            cb.RegisterType<DependsByProp>().SingleInstance().PropertiesAutowired(true);

            var c = cb.Build();
            var dbp = c.Resolve<DependsByProp>();

            Assert.IsNotNull(dbp.Dep);
            Assert.IsNotNull(dbp.Dep.Dep);
            Assert.AreSame(dbp, dbp.Dep.Dep);
        }

        [Test]
        public void CtorPropDependencyOkOrder2()
        {
            var cb = new ContainerBuilder();
            cb.RegisterType<DependsByCtor>().SingleInstance();
            cb.RegisterType<DependsByProp>().SingleInstance().PropertiesAutowired(true);

            var c = cb.Build();
            var dbc = c.Resolve<DependsByCtor>();

            Assert.IsNotNull(dbc.Dep);
            Assert.IsNotNull(dbc.Dep.Dep);
            Assert.AreSame(dbc, dbc.Dep.Dep);
        }

        [Test]
        [ExpectedException(typeof(DependencyResolutionException))]
        public void CtorPropDependencyFactoriesOrder1()
        {
            var cb = new ContainerBuilder();
            cb.RegisterType<DependsByCtor>();
            cb.RegisterType<DependsByProp>().PropertiesAutowired(true);

            var c = cb.Build();
            c.Resolve<DependsByProp>();
        }

        [Test]
        [ExpectedException(typeof(DependencyResolutionException))]
        public void CtorPropDependencyFactoriesOrder2()
        {
            var cb = new ContainerBuilder();
            var ac = 0;
            // ReSharper disable AccessToModifiedClosure
            cb.RegisterType<DependsByCtor>().OnActivating(e => { ++ac; });
            // ReSharper restore AccessToModifiedClosure
            cb.RegisterType<DependsByProp>().OnActivating(e => { ++ac; })
                .PropertiesAutowired(true);

            var c = cb.Build();
            c.Resolve<DependsByCtor>();

            Assert.AreEqual(2, ac);
        }


        [Test]
        public void ActivatingArgsSuppliesParameters()
        {
            const int provided = 12;
            var passed = 0;

            var builder = new ContainerBuilder();
            builder.RegisterType<object>()
                .OnActivating(e => passed = e.Parameters.TypedAs<int>());
            var container = builder.Build();

            container.Resolve<object>(TypedParameter.From(provided));
            Assert.AreEqual(provided, passed);
        }

        [Test]
        public void ActivatedArgsSuppliesParameters()
        {
            const int provided = 12;
            var passed = 0;

            var builder = new ContainerBuilder();
            builder.RegisterType<object>()
                .OnActivated(e => passed = e.Parameters.TypedAs<int>());
            var container = builder.Build();

            container.Resolve<object>(TypedParameter.From(provided));
            Assert.AreEqual(provided, passed);
        }
    }
}
