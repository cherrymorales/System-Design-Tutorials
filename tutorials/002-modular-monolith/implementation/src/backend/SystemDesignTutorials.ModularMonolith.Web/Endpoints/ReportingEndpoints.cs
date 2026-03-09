using System.Security.Claims;
using SystemDesignTutorials.ModularMonolith.Application.Modules;

namespace SystemDesignTutorials.ModularMonolith.Web.Endpoints;

internal static class ReportingEndpoints
{
    public static void MapReportingEndpoints(this IEndpointRouteBuilder app)
    {
        var reports = app.MapGroup("/reports");
        reports.MapGet("/summary", GetSummaryAsync);
    }

    private static async Task<IResult> GetSummaryAsync(ClaimsPrincipal user, IReportingModule reportingModule, CancellationToken cancellationToken)
    {
        if (!AccessControl.CanViewReports(user))
        {
            return AccessControl.Forbidden("Only operations managers can view reporting dashboards.");
        }

        return Results.Ok(await reportingModule.GetSummaryAsync(cancellationToken));
    }
}

