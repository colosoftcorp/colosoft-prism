namespace Colosoft.Prism.Modularity
{
    public interface IModuleTypeLoaderObserver
    {
        ModuleTypeLoaderStage Stage { get; }

        IMessageFormattable Message { get; set; }

        long Total { get; set; }

        long Current { get; set; }

        void SetStage(ModuleTypeLoaderStage stage);
    }
}
