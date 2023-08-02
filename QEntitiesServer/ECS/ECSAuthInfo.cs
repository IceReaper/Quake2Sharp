using System.ComponentModel.DataAnnotations;

namespace QEntitiesServer.ECS;

public class ECSAuthInfo
{
    [Required(ErrorMessage = nameof(TenantId) + " is required.")]
    public string TenantId { get; set; }

    [Required(ErrorMessage = nameof(ClientId) + " is required.")]
    public string ClientId { get; set; }

    [Required(ErrorMessage = nameof(ClientSecret) + " is required.")]
    public string ClientSecret { get; set; }
}