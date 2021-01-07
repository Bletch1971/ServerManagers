using System.Threading.Tasks;

namespace ServerManagerTool.Common.Interfaces
{
    public interface IAsyncDisposable
    {
        Task DisposeAsync();
    }
}
