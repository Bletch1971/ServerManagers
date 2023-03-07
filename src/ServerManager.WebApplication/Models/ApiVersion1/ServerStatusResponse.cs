using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace ServerManager.WebApplication.Models.ApiVersion1;

public class ServerStatusResponse
{
    /// <summary>
    /// True if the server is available; otherwise false.
    /// </summary>
    [Required]
    [Description("True if the server is available; otherwise false.")]
    public string Available { get; set; } = false.ToString();
}
