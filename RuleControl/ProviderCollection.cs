using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBLint.RuleControl;

namespace DBLint.RuleControl
{
    public class ProviderCollection : IProviderCollection
    {
        private Dictionary<Type, IProvider> providers = new Dictionary<Type, IProvider>();

        private IProvider GetProvider(Type providerType)
        {
            if (!this.providers.ContainsKey(providerType))
                return null;

            IProvider provider = this.providers[providerType];
            return provider;
        }

        public void AddProvider(IProvider provider)
        {
            var type = provider.GetType();
            if (providers.ContainsKey(type))
                throw new Exception("A provider of type " + provider.GetType() + " already exist");
            this.providers.Add(type, provider);
        }

        public T GetProvider<T>() where T : IProvider
        {
            var providerType = typeof(T);
            var provider = GetProvider(providerType);
            if (provider == null)
                return default(T);
            else
                return (T)provider;
        }
    }
}
