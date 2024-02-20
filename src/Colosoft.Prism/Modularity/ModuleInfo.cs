using System;

namespace Colosoft.Prism.Modularity
{
    public class ModuleInfo : global::Prism.Modularity.ModuleInfo
    {
        public Colosoft.Modularity.IModuleInfo InnerInfo { get; }

        public Type Type { get; set; }

        public Reflection.TypeName TypeName { get; set; }

        public ModuleInfo(Colosoft.Modularity.IModuleInfo info)
        {
            this.InnerInfo = info ?? throw new ArgumentNullException(nameof(info));
            this.ModuleName = info.FullName;
            this.ModuleType = info.ModuleType;

            foreach (var i in info.Dependencies)
            {
                this.DependsOn.Add(i);
            }
        }
    }
}
