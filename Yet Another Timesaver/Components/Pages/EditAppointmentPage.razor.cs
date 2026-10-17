using Microsoft.AspNetCore.Components;

namespace Yet_Another_Timesaver.Components.Pages;

public partial class EditAppointmentPage : ComponentBase
{
    [Parameter]
    public Appointment Appointment { get; set; }

    Appointment model = new Appointment();

    protected override void OnParametersSet()
    {
        model = Appointment;
    }

    void OnSubmit(Appointment model)
    {
        DialogService.Close(model);
    }
}