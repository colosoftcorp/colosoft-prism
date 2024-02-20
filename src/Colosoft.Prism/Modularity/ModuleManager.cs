using Colosoft.Modularity;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Colosoft.Prism.Modularity
{
    public class ModuleManager : global::Prism.Modularity.IModuleManager, IDisposable
    {
        private readonly ModuleInitializer initializer;
        private readonly global::Prism.Modularity.IModuleCatalog moduleCatalog;
        private readonly IModuleTypeLoaderObserver moduleTypeLoaderObserver;
        private readonly Mef.AssemblyRepositoryCatalog aggregateCatalog;
        private readonly HashSet<global::Prism.Modularity.IModuleTypeLoader> subscribedToModuleTypeLoaders;
        private readonly Lazy<Reflection.IAssemblyInfoRepository> assemblyInfoRepository;

        private readonly List<global::Prism.Modularity.IModule> modules = new List<global::Prism.Modularity.IModule>();
        private readonly List<global::Prism.Modularity.IModuleInfo> infos = new List<global::Prism.Modularity.IModuleInfo>();

        private IEnumerable<global::Prism.Modularity.IModuleTypeLoader> typeLoaders;

        public List<global::Prism.Modularity.IModule> Modules
        {
            get { return this.modules; }
        }

        public IEnumerable<global::Prism.Modularity.IModuleInfo> ModuleInfos => this.infos;

        IEnumerable<global::Prism.Modularity.IModuleInfo> global::Prism.Modularity.IModuleManager.Modules => this.ModuleInfos;

        public event EventHandler<global::Prism.Modularity.LoadModuleCompletedEventArgs> LoadModuleCompleted;

        public event EventHandler<global::Prism.Modularity.ModuleDownloadProgressChangedEventArgs> ModuleDownloadProgressChanged;

        public IEnumerable<global::Prism.Modularity.IModuleTypeLoader> ModuleTypeLoaders
        {
            get
            {
                if (this.typeLoaders == null)
                {
                    var loader = new ModuleTypeLoader(
                        this.moduleTypeLoaderObserver,
                        this.assemblyInfoRepository,
                        this.aggregateCatalog);

                    this.typeLoaders = new List<global::Prism.Modularity.IModuleTypeLoader>()
                    {
                        loader,
                    };
                }

                return this.typeLoaders;
            }

            set
            {
                this.typeLoaders = value;
            }
        }

        protected global::Prism.Modularity.IModuleCatalog ModuleCatalog
        {
            get
            {
                return this.moduleCatalog;
            }
        }

        public ModuleManager(
            global::Prism.Modularity.IModuleInitializer moduleInitializer,
            global::Prism.Modularity.IModuleCatalog moduleCatalog,
            IModuleTypeLoaderObserver moduleTypeLoaderObserver,
            Lazy<Reflection.IAssemblyInfoRepository> assemblyInfoRepository,
            Mef.AssemblyRepositoryCatalog aggregateCatalog)
        {
            if (moduleInitializer is null)
            {
                throw new ArgumentNullException(nameof(moduleInitializer));
            }

            this.subscribedToModuleTypeLoaders = new HashSet<global::Prism.Modularity.IModuleTypeLoader>();

            this.initializer = moduleInitializer as Prism.Modularity.ModuleInitializer;
            this.moduleCatalog = moduleCatalog ?? throw new ArgumentNullException(nameof(moduleCatalog));
            this.moduleTypeLoaderObserver = moduleTypeLoaderObserver;
            this.assemblyInfoRepository = assemblyInfoRepository ?? throw new ArgumentNullException(nameof(assemblyInfoRepository));
            this.aggregateCatalog = aggregateCatalog;

            if (this.initializer != null)
            {
                this.initializer.Created += this.Initializer_Created;
            }
        }

        public void LoadModule(string moduleName)
        {
            var source = this.moduleCatalog.Modules.Where(f => f.ModuleName == moduleName);

            if (source.Count() != 1)
            {
                throw new global::Prism.Modularity.ModuleNotFoundException(
                    moduleName,
                    string.Format(
                        System.Globalization.CultureInfo.CurrentCulture,
                        Properties.Resources.ModuleNotFound,
                        moduleName));
            }

            var moduleInfos = this.moduleCatalog.CompleteListWithDependencies(source);
            this.LoadModuleTypes(moduleInfos);
        }

        public void Run()
        {
            this.moduleCatalog.Initialize();
            this.LoadModulesWhenAvailable();
        }

        protected void LoadModulesThatAreReadyForLoad()
        {
            bool flag = true;
            while (flag)
            {
                flag = false;

                foreach (var info in this.moduleCatalog.Modules.Where(f => f.State == global::Prism.Modularity.ModuleState.ReadyForInitialization))
                {
                    if ((info.State != global::Prism.Modularity.ModuleState.Initialized) &&
                         this.AreDependenciesLoaded(info))
                    {
                        info.State = global::Prism.Modularity.ModuleState.Initializing;
                        this.InitializeModule(info);
                        flag = true;
                        break;
                    }
                }
            }
        }

        protected virtual bool ModuleNeedsRetrieval(global::Prism.Modularity.IModuleInfo moduleInfo)
        {
            if (moduleInfo is null)
            {
                throw new ArgumentNullException(nameof(moduleInfo));
            }

            if (moduleInfo.State == global::Prism.Modularity.ModuleState.NotStarted)
            {
                bool flag = Type.GetType(moduleInfo.ModuleType) != null;
                if (flag)
                {
                    moduleInfo.State = global::Prism.Modularity.ModuleState.ReadyForInitialization;
                }

                return !flag;
            }

            return false;
        }

        protected virtual void HandleModuleTypeLoadingError(global::Prism.Modularity.IModuleInfo moduleInfo, Exception exception)
        {
            if (moduleInfo is null)
            {
                throw new ArgumentNullException(nameof(moduleInfo));
            }

            var exception2 = exception as global::Prism.Modularity.ModuleTypeLoadingException;
            if (exception2 == null)
            {
                exception2 = new global::Prism.Modularity.ModuleTypeLoadingException(moduleInfo.ModuleName, exception?.Message, exception);
            }

            throw exception2;
        }

        private void LoadModulesWhenAvailable()
        {
            var catalogModules = this.moduleCatalog.Modules.Where(f => f.InitializationMode == global::Prism.Modularity.InitializationMode.WhenAvailable);
            var moduleInfos = this.moduleCatalog.CompleteListWithDependencies(catalogModules).ToArray();

            if (moduleInfos.Length > 0)
            {
                this.LoadModuleTypes(moduleInfos);
            }
        }

        private void LoadModuleTypes(IEnumerable<global::Prism.Modularity.IModuleInfo> moduleInfos)
        {
            if (moduleInfos != null)
            {
                foreach (var info in moduleInfos)
                {
                    this.infos.Add(info);

                    if (info.State == global::Prism.Modularity.ModuleState.NotStarted && this.ModuleNeedsRetrieval(info))
                    {
                        info.State = this.BeginRetrievingModule(info)
                            ? global::Prism.Modularity.ModuleState.NotStarted
                            : global::Prism.Modularity.ModuleState.ReadyForInitialization;
                    }
                }

                this.LoadModulesThatAreReadyForLoad();
            }
        }

        private void InitializeModule(global::Prism.Modularity.IModuleInfo moduleInfo)
        {
            if (moduleInfo.State == global::Prism.Modularity.ModuleState.Initializing)
            {
                try
                {
                    this.initializer.Initialize(moduleInfo);
                }
                catch (Exception ex)
                {
                    this.RaiseLoadModuleCompleted(moduleInfo, ex);
                    moduleInfo.State = global::Prism.Modularity.ModuleState.NotStarted;
                    return;
                }

                moduleInfo.State = global::Prism.Modularity.ModuleState.Initialized;
                this.RaiseLoadModuleCompleted(moduleInfo, null);
            }
        }

        private void RaiseLoadModuleCompleted(global::Prism.Modularity.LoadModuleCompletedEventArgs e)
        {
            this.LoadModuleCompleted?.Invoke(this, e);
        }

        private void RaiseLoadModuleCompleted(global::Prism.Modularity.IModuleInfo moduleInfo, Exception error)
        {
            this.RaiseLoadModuleCompleted(new global::Prism.Modularity.LoadModuleCompletedEventArgs(moduleInfo, error));
        }

        private void RaiseModuleDownloadProgressChanged(global::Prism.Modularity.ModuleDownloadProgressChangedEventArgs e)
        {
            this.ModuleDownloadProgressChanged?.Invoke(this, e);
        }

        private bool AreDependenciesLoaded(global::Prism.Modularity.IModuleInfo moduleInfo)
        {
            var dependentModules = this.moduleCatalog.GetDependentModules(moduleInfo);

            return (dependentModules == null) ||
                   (!dependentModules.Any(f => f.State != global::Prism.Modularity.ModuleState.Initialized));
        }

        private bool BeginRetrievingModule(global::Prism.Modularity.IModuleInfo moduleInfo)
        {
            var info = moduleInfo;

            global::Prism.Modularity.IModuleTypeLoader typeLoaderForModule = null;

            try
            {
                typeLoaderForModule = this.GetTypeLoaderForModule(info);
            }
            catch (Exception ex)
            {
                this.IModuleTypeLoader_LoadModuleCompleted(this, new global::Prism.Modularity.LoadModuleCompletedEventArgs(moduleInfo, ex));
                return false;
            }

            if (typeLoaderForModule == null)
            {
                return false;
            }

            info.State = global::Prism.Modularity.ModuleState.LoadingTypes;
            if (!this.subscribedToModuleTypeLoaders.Contains(typeLoaderForModule))
            {
                typeLoaderForModule.ModuleDownloadProgressChanged += this.IModuleTypeLoader_ModuleDownloadProgressChanged;
                typeLoaderForModule.LoadModuleCompleted += this.IModuleTypeLoader_LoadModuleCompleted;
                this.subscribedToModuleTypeLoaders.Add(typeLoaderForModule);
            }

            typeLoaderForModule.LoadModuleType(moduleInfo);

            return true;
        }

        private global::Prism.Modularity.IModuleTypeLoader GetTypeLoaderForModule(global::Prism.Modularity.IModuleInfo moduleInfo)
        {
            return this.ModuleTypeLoaders.FirstOrDefault(loader => loader.CanLoadModuleType(moduleInfo));
        }

        private void IModuleTypeLoader_LoadModuleCompleted(object sender, global::Prism.Modularity.LoadModuleCompletedEventArgs e)
        {
            if (e.Error == null)
            {
                if ((e.ModuleInfo.State != global::Prism.Modularity.ModuleState.Initializing) &&
                    (e.ModuleInfo.State != global::Prism.Modularity.ModuleState.Initialized))
                {
                    e.ModuleInfo.State = global::Prism.Modularity.ModuleState.ReadyForInitialization;
                }

                this.LoadModulesThatAreReadyForLoad();
            }
            else
            {
                this.RaiseLoadModuleCompleted(e);
                if (!e.IsErrorHandled)
                {
                    this.HandleModuleTypeLoadingError(e.ModuleInfo, e.Error);
                }
            }
        }

        private void IModuleTypeLoader_ModuleDownloadProgressChanged(object sender, global::Prism.Modularity.ModuleDownloadProgressChangedEventArgs e)
        {
            this.RaiseModuleDownloadProgressChanged(e);
        }

        private void Initializer_Created(object sender, ModuleCreatedArgs e)
        {
            if (e.ModuleInfo != null)
            {
                this.infos.Add(e.ModuleInfo);
            }

            if (e.Module != null)
            {
                this.modules.Add(e.Module);
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            foreach (var loader in this.ModuleTypeLoaders)
            {
                IDisposable disposable = loader as IDisposable;
                if (disposable != null)
                {
                    disposable.Dispose();
                }
            }

            if (this.initializer != null)
            {
                this.initializer.Created -= this.Initializer_Created;
            }
        }
    }
}
