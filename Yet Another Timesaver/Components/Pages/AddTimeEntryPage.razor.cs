using Microsoft.AspNetCore.Components;
using Yet_Another_Timesaver.Backend;

namespace Yet_Another_Timesaver.Components.Pages;

public partial class AddTimeEntryPage : ComponentBase
{
    [Parameter]
    public DateTime Start { get; set; }

    [Parameter]
    public DateTime End { get; set; }

    TimeEntry model = new TimeEntry();

    protected override void OnParametersSet()
    {
        model.StartTime = Start;
        model.EndTime = End;
    }

    void OnSubmit(TimeEntry model)
    {
        DialogService.Close(model);
    }
}
