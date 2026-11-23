using Microsoft.AspNetCore.Components;
using Yet_Another_Timesaver.Backend;

namespace Yet_Another_Timesaver.Components.Pages;

public partial class WeekViewSettingsDialog : ComponentBase
{
    [Parameter]
    public WeekViewSettings Settings { get; set; } = WeekViewSettings.Default;

    private WeekViewSettings model = new();

    private static readonly DayOfWeek[] OrderedDays =
    {
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
        DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday
    };

    private IEnumerable<DayOfWeek> AvailableStartDays =>
        model.WorkWeekOnly
            ? OrderedDays.Where(day => day != DayOfWeek.Saturday && day != DayOfWeek.Sunday)
            : OrderedDays;

    protected override void OnParametersSet()
    {
        model = new WeekViewSettings
        {
            StartDay = Settings.StartDay,
            WorkWeekOnly = Settings.WorkWeekOnly
        };
    }

    private void OnWorkWeekOnlyChanged(bool value)
    {
        if (value && (model.StartDay == DayOfWeek.Saturday || model.StartDay == DayOfWeek.Sunday))
        {
            model.StartDay = DayOfWeek.Monday;
        }
    }

    private void OnSaveClick()
    {
        DialogService.Close(model);
    }

    private void OnCancelClick()
    {
        DialogService.Close();
    }
}
