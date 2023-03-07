using System.Runtime.Serialization;

namespace ServerManager.WebApplication.Models.Data;

[DataContract]
public class ManagerCode
{
    [DataMember]
    public string Name { get; set; } = string.Empty;
    [DataMember]
    public string Code { get; set; } = string.Empty;
}
