namespace CipherLink.Setup
{
    public interface ISetupScript
    {
        string Name { get; }
        string Description { get; }
        string Usage { get; }
        void ExecuteInteractive();
        void ExecuteCommand(string[] args);
    }
}
