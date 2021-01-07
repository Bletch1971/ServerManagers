namespace ServerManagerTool.Plugin.Discord
{
    internal interface IBindable
    {
        bool HasChanges { get; set; }

        bool HasAnyChanges { get; }

        void CommitChanges();

        void BeginUpdate();

        void EndUpdate();
    }
}
