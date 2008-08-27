﻿using System;
using System.Collections.Generic;

namespace Autofac
{
    public class LifetimeScope : Disposable, ISharingLifetimeScope
    {
        IComponentRegistry _componentRegistry;
        ISharingLifetimeScope _root; // Root optimises singleton lookup without traversal
        ISharingLifetimeScope _parent;
        IDisposer _disposer = new Disposer();
        IDictionary<Guid, object> _sharedInstances = new Dictionary<Guid, object>();

        protected LifetimeScope(IComponentRegistry componentRegistry, LifetimeScope parent)
            : this(componentRegistry)
        {
            _parent = Enforce.ArgumentNotNull(parent, "parent");
            _root = _parent.RootLifetimeScope;
        }

        public LifetimeScope(IComponentRegistry componentRegistry)
        {
            _componentRegistry = Enforce.ArgumentNotNull(componentRegistry, "componentRegistry");
            _root = this;
        }

        public ILifetimeScope BeginLifetimeScope()
        {
            return new LifetimeScope(_componentRegistry, this);
        }

        public bool TryResolve(Service service, IEnumerable<Parameter> parameters, out object instance)
        {
            var operation = new ResolveOperation(this, _componentRegistry);
            return operation.TryResolve(service, parameters, out instance);
        }

        public ISharingLifetimeScope ParentLifetimeScope
        {
            get { return _parent; }
        }

        public ISharingLifetimeScope RootLifetimeScope
        {
            get { return _root; }
        }

        public bool TryGetSharedInstance(Guid id, out object result)
        {
            return _sharedInstances.TryGetValue(id, out result);
        }

        public void AddSharedInstance(Guid id, object newInstance)
        {
            Enforce.ArgumentNotNull(newInstance, "newInstance");

            _sharedInstances.Add(id, newInstance);
        }

        public IDisposer Disposer
        {
            get { return _disposer; }
        }

        public IComponentRegistry ComponentRegistry
        {
            get { return _componentRegistry; }
        }
    }
}