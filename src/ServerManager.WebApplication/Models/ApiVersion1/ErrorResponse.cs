using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace ServerManager.WebApplication.Models.ApiVersion1;

public class ErrorResponse
{
    /// <summary>
    /// List of errors.
    /// </summary>
    [Required]
    [Description("List of errors.")]
    public ICollection<string> Errors { get; set; } = new List<string>();
}
