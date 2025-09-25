using System.Net;
using System.Text;
using ExampleApi.Infrastructure;
using ExampleApi.Models;
using ExampleApi.Services;
using IeuanWalker.MinimalApi.Endpoints;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ExampleApi.Endpoints.Todos.GetExport;

public class GetExportEndpoint : IEndpointWithoutRequest<Results<FileContentHttpResult, NoContent>>
{
	readonly ITodoStore _todoStore;

	public GetExportEndpoint(ITodoStore todoStore)
	{
		_todoStore = todoStore;
	}

	public static void Configure(RouteHandlerBuilder builder)
	{
		builder
			.Group<TodoEndpointGroup>()
			.Get("/export")
			.WithSummary("Export todos")
			.WithDescription("Exports all todos as a downloadable HTML file. Returns 204 No Content if no todos exist.")
			.WithResponse<string>(StatusCodes.Status200OK, "Downloadable HTML file containing all todos in a formatted table", "text/html")
			.Version(1.0);
	}

	public async Task<Results<FileContentHttpResult, NoContent>> HandleAsync(CancellationToken ct)
	{
		IEnumerable<Todo> result = await _todoStore.GetAllAsync(ct);

		if (!result.Any())
		{
			return TypedResults.NoContent();
		}

		string fileName = $"todos-{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}.html";
		string htmlContent = ExportToHtml(result);

		byte[] fileBytes = Encoding.UTF8.GetBytes(htmlContent);

		return TypedResults.File(fileBytes, "text/html", fileName);
	}

	static string ExportToHtml(IEnumerable<Todo> todo)
	{
		string rows = string.Join(Environment.NewLine, todo.Select(t => $@"
			<tr>
				<td>{t.Id}</td>
				<td>{WebUtility.HtmlEncode(t.Title)}</td>
				<td>{WebUtility.HtmlEncode(t.Description)}</td>
				<td>{(t.IsCompleted ? "Yes" : "No")}</td>
				<td>{t.CreatedAt:yyyy-MM-dd HH:mm:ss}</td>
				<td>{(t.UpdatedAt.HasValue ? t.UpdatedAt.Value.ToString("yyyy-MM-dd HH:mm-ss") : "")}</td>
			</tr>"));

		return $$"""

		<!DOCTYPE html>
		<html lang="en">
		<head>
			<meta charset="UTF-8">
			<meta name="viewport" content="width=device-width, initial-scale=1.0">
			<title>Todo Export</title>
			<style>
				table {
					width: 100%;
					border-collapse: collapse;
				}
				th, td {
					border: 1px solid #ddd;
					padding: 8px;
				}
				th {
					background-color: #f2f2f2;
					text-align: left;
				}
			</style>
		</head>
		<body>
			<h1>Todo Export</h1>
			<table>
				<thead>
					<tr>
						<th>ID</th>
						<th>Title</th>
						<th>Description</th>
						<th>Is Completed</th>
						<th>Created At</th>
						<th>Updated At</th>
					</tr>
				</thead>
				<tbody>
					{{rows}}
				</tbody>
			</table>
		</body>
		</html>
""";

	}
}
