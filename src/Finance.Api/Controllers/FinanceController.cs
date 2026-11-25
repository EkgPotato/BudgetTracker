using System.Security.Claims;
using Finance.Api.Contracts.Finance;
using Finance.Api.Managers;
using Finance.Api.Validators;
using Finance.Localization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Finance.Api.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public sealed class FinanceController(IFinanceManager manager, FinanceRequestValidator validator) : ControllerBase
{
    private string? OwnerId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

    [HttpGet("budget-spaces")]
    public Task<IActionResult> GetSpaces(CancellationToken token)
    {
        return WithOwnerAsync(ownerId => manager.GetSpacesAsync(ownerId, token));
    }

    [HttpPost("budget-spaces")]
    public async Task<IActionResult> CreateSpace(BudgetSpaceRequest request, CancellationToken token)
    {
        var errors = validator.Validate(request);
        if (errors.Count > 0) return ValidationProblem(new ValidationProblemDetails(errors));
        if (OwnerId is null) return Unauthorized();

        var response = await manager.CreateSpaceAsync(OwnerId, request, token);
        return Created($"/api/budget-spaces/{response.Id}", response);
    }

    [HttpGet("accounts")]
    public Task<IActionResult> GetAccounts(CancellationToken token)
    {
        return WithOwnerAsync(ownerId => manager.GetAccountsAsync(ownerId, token));
    }

    [HttpPost("accounts")]
    public async Task<IActionResult> CreateAccount(AccountRequest request, CancellationToken token)
    {
        var errors = validator.Validate(request);
        if (errors.Count > 0) return ValidationProblem(new ValidationProblemDetails(errors));
        if (OwnerId is null) return Unauthorized();

        var response = await manager.CreateAccountAsync(OwnerId, request, token);
        return response is null ? NotFound() : Created($"/api/accounts/{response.Id}", response);
    }

    [HttpGet("categories")]
    public Task<IActionResult> GetCategories(CancellationToken token)
    {
        return WithOwnerAsync(ownerId => manager.GetCategoriesAsync(ownerId, token));
    }

    [HttpPost("categories")]
    public async Task<IActionResult> CreateCategory(CategoryRequest request, CancellationToken token)
    {
        var errors = validator.Validate(request);
        if (errors.Count > 0) return ValidationProblem(new ValidationProblemDetails(errors));
        if (OwnerId is null) return Unauthorized();

        var response = await manager.CreateCategoryAsync(OwnerId, request, token);
        return response is null ? NotFound() : Created($"/api/categories/{response.Id}", response);
    }

    [HttpGet("transactions")]
    public Task<IActionResult> GetTransactions(CancellationToken token)
    {
        return WithOwnerAsync(ownerId => manager.GetTransactionsAsync(ownerId, token));
    }

    [HttpGet("transactions/page")]
    public async Task<IActionResult> GetTransactionPage([FromQuery] TransactionPageQuery query, CancellationToken token)
    {
        if (OwnerId is null) return Unauthorized();
        if (query.Offset < 0 || query.Limit is < 1 or > 100 || query.From > query.To ||
            query.Kinds.Any(kind => !Enum.IsDefined(kind))) return BadRequest();
        return Ok(await manager.GetTransactionPageAsync(OwnerId, query, token));
    }

    [HttpPost("transactions")]
    public async Task<IActionResult> CreateTransaction(TransactionRequest request, CancellationToken token)
    {
        var errors = validator.Validate(request);
        if (errors.Count > 0) return ValidationProblem(new ValidationProblemDetails(errors));
        if (OwnerId is null) return Unauthorized();

        var response = await manager.CreateTransactionAsync(OwnerId, request, token);
        return response is null ? BadRequest() : Created($"/api/transactions/{response.Id}", response);
    }

    [HttpPost("transfers")]
    public async Task<IActionResult> CreateTransfer(TransferRequest request, CancellationToken token)
    {
        var errors = validator.Validate(request);
        if (errors.Count > 0) return ValidationProblem(new ValidationProblemDetails(errors));
        if (OwnerId is null) return Unauthorized();

        var response = await manager.CreateTransferAsync(OwnerId, request, token);
        return response is null ? BadRequest() : Created($"/api/transfers/{response.Id}", response);
    }

    [HttpGet("dashboard")]
    public Task<IActionResult> GetDashboard(CancellationToken token)
    {
        return WithOwnerAsync(ownerId => manager.DashboardAsync(ownerId, token));
    }

    [HttpGet("reports/monthly")]
    public async Task<IActionResult> GetMonthlyReport(int year, int month, CancellationToken token)
    {
        if (OwnerId is null) return Unauthorized();
        if (year is < 1 or > 9999 || month is < 1 or > 12 || (year == 9999 && month == 12)) return BadRequest();

        return Ok(await manager.MonthlyReportAsync(OwnerId, year, month, token));
    }

    [HttpGet("reports")]
    public async Task<IActionResult> GetReport([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken token)
    {
        if (OwnerId is null) return Unauthorized();
        if (from is null || to is null || from > to)
            return BadRequest(new { message = ValidationMessages.Get("DateRangeInvalid") });

        return Ok(await manager.ReportAsync(OwnerId, from.Value, to.Value, token));
    }

    private async Task<IActionResult> WithOwnerAsync<T>(Func<string, Task<T>> action)
    {
        if (OwnerId is null) return Unauthorized();
        return Ok(await action(OwnerId));
    }
}
