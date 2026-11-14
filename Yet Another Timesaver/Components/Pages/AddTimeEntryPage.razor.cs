using Microsoft.AspNetCore.Components;
using Yet_Another_Timesaver.Backend;

namespace Yet_Another_Timesaver.Components.Pages;

public partial class AddTimeEntryPage : ComponentBase
{
    [Parameter]
    public DateTime Start { get; set; }

    [Parameter]
    public DateTime End { get; set; }

    private readonly TimeEntry model = new();
    private string? errorMessage;

    protected override void OnParametersSet()
    {
        model.StartTime = Start;
        model.EndTime = End;
    }

    private void OnSubmit(TimeEntry model)
    {
        if (model.EndTime.HasValue && model.EndTime.Value <= model.StartTime)
        {
            errorMessage = "The end time must be after the start time.";
            return;
        }

        errorMessage = null;
        DialogService.Close(model);
    }
}
