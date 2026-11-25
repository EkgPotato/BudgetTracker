namespace Finance.Api.Contracts.Finance;

public sealed record MonthlyReportResponse(
    int Year,
    int Month,
    decimal Income,
    decimal Expenses,
    IReadOnlyList<ReportCategoryResponse> Categories);
