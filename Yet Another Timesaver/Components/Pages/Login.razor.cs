using Microsoft.AspNetCore.Components;

namespace Yet_Another_Timesaver.Components.Pages;

public partial class Login
{
    private string userName = "";
    private string password = "";

    [SupplyParameterFromQuery(Name = "error")]
    public string? Error { get; set; }
}