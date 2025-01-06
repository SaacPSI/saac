namespace SAAC.PipelineServices
{
    public interface IRebootingComponent
    {
        public abstract Dictionary<string, object> StoreData();
        public abstract void RestoreData(Dictionary<string, object> data);
    }
}
