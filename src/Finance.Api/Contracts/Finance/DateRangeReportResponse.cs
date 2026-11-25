namespace Finance.Api.Contracts.Finance;

public sealed record DateRangeReportResponse(
    DateOnly From,
    DateOnly To,
    decimal Income,
    decimal Expenses,
    IReadOnlyList<ReportCategoryResponse> Categories);
