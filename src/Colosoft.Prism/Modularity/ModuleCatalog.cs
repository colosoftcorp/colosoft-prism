using System;
using System.Threading;

namespace Colosoft.Prism.Modularity
{
    public class ModuleCatalog : global::Prism.Modularity.ModuleCatalog
    {
        private readonly Colosoft.Modularity.IModuleProvider moduleProvider;
        private readonly Lazy<IModuleCatalogObserver> observer;
        private readonly string uiContextFullName;

        public ModuleCatalog(
            string uiContextFullName,
            Colosoft.Modularity.IModuleProvider moduleProvider,
            Lazy<IModuleCatalogObserver> observer)
        {
            this.uiContextFullName = uiContextFullName ?? throw new ArgumentNullException(nameof(uiContextFullName));
            this.moduleProvider = moduleProvider ?? throw new ArgumentNullException(nameof(moduleProvider));
            this.observer = observer ?? throw new ArgumentNullException(nameof(observer));
        }

        public override void Initialize()
        {
            var profile = Thread.CurrentPrincipal?.Identity;

            this.observer.Value.OnLoadingProviderModules();

            try
            {
                var modules = this.moduleProvider.GetModules(profile, this.uiContextFullName);

                foreach (var i in modules)
                {
                    this.AddModule(new ModuleInfo(i));
                }
            }
            catch (Exception ex)
            {
                var message = ResourceMessageFormatter.Create(
                    () => Properties.Resources.ModuleCatalog_FailOnGetModules);
                this.observer.Value.FailOnLoadingProviderModules(message, ex);
            }

            base.Initialize();
        }
    }
}
