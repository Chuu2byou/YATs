using Microsoft.AspNetCore.Components;
using Yet_Another_Timesaver.Backend;

namespace Yet_Another_Timesaver.Components.Pages;

public partial class EditTimeEntryPage : ComponentBase
{
    [Parameter]
    public TimeEntry TimeEntry { get; set; }

    TimeEntry model = new TimeEntry();

    protected override void OnParametersSet()
    {
        model = TimeEntry;
    }

    void OnSubmit(TimeEntry model)
    {
        DialogService.Close(model);
    }
}
