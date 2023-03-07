namespace ServerManager.WebApplication.Services;

public interface IServerQueryService
{
    bool CheckServerStatus(string managerCode, string managerVersion, string ipString, int port);
}
