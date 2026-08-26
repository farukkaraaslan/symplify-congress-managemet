namespace Symplify.BackOffice.WebUI.Models.Home;

public sealed class HomeIndexViewModel
{
    public string DisplayName { get; set; } = string.Empty;

    public ActiveCongressSummaryViewModel? ActiveCongress { get; set; }
}

public sealed class ActiveCongressSummaryViewModel
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public string? VenueName { get; set; }
}
