using Microsoft.AspNetCore.Components;

namespace Yet_Another_Timesaver.Components.Pages;

public partial class AddAppointmentPage : ComponentBase
{
    [Parameter]
    public DateTime Start { get; set; }

    [Parameter]
    public DateTime End { get; set; }

    Appointment model = new Appointment();

    protected override void OnParametersSet()
    {
        model.Start = Start;
        model.End = End;
    }

    void OnSubmit(Appointment model)
    {
        DialogService.Close(model);
    }
}