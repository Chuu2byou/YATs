using Microsoft.AspNetCore.Components;
using Radzen;
using Yet_Another_Timesaver.Backend;

namespace Yet_Another_Timesaver.Components.Pages;

/// <summary>Provides the time entry edit and delete dialog.</summary>
public partial class EditTimeEntryPage : ComponentBase
{
    [Parameter] public TimeEntry TimeEntry { get; set; } = default!;

    private TimeEntry model = new();
    private string? errorMessage;

    protected override void OnParametersSet()
    {
        model = TimeEntry;
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

    private async Task OnDeleteClick()
    {
        var confirmed = await DialogService.Confirm(
            "Do you really want to delete this time entry?",
            "Delete TimeEntry",
            new ConfirmOptions { OkButtonText = "Delete", CancelButtonText = "Cancel" });

        if (confirmed == true)
        {
            DialogService.Close(new TimeEntryDeleteRequest(model.Id));
        }
    }
}
