using System;

namespace Colosoft.Prism.Modularity
{
    public interface IModuleCatalogObserver
    {
        void OnLoadingProviderModules();

        void FailOnLoadingProviderModules(IMessageFormattable message, Exception exception);
    }
}
