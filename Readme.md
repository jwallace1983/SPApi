# SPApi

"SPApi" is an open-source project to enable stored-procedures to dynamically run as API endpoints. This is
intended as a lightweight API solution to rapidly develop data-driven APIs with rich front-end applications
so that new endpoints can be added with only new stored procedures.

To add a new endpoint, create a stored procedure with specific extended properties (no custom tables or schemas
are required to support this solution). Easily enable as middleware for a Web API and integrate with any existing
.NET Web API Authentication/Claims.

## Enable the SPApi

1. Configure the dependency injection by calling `AddSPApi()` extension method when configuring the services.

```
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSPApi();
```

2. Ensure that an IDbConnection available for dependency injection to the target database:

```
builder.Services.AddTransient<IDbConnection>(services =>
{
    var config = services.GetService<IConfiguration>();
    return new SqlConnection(config.GetConnectionString("DefaultConnection"));
});
```

3. Enable the middleware by calling `UseSPApi()` extension method on the application object.

```
var app = builder.Build();
// Configure authentication and authorization
app.UseSPApi();
```

## Customize available settings

Use the settings when configuring dependency injection to customize usage of the tool, such as:

* Change the endpoint for the api
* Enable help documentation for endpoints
* Enable non-https requests
* Customize error and not found handling

For example, to enable help documentation, but only when a pre-shared header for "x-spapi-key" matches:

```
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSPApi(settings =>
{
    settings.EnableHelp = true;
    settings.HelpKey = "my-secret-key";
	settings.UseErrorHandler((httpContext, ex) => { /* Custom error handling */ });
});
```

## Sample API Request

The following sample API request will call a specific stored procedure with provided parameters.

Set the `Context` to `help` to review documentation for the endpoint.

```
POST: https://path-to-site/api/data
{
    "schema": "dbo",
    "object": "Test_Select",
    "parameters": {
        "Number": 1
    },
    "Context": ""
}
```

## Sample API Response

When documentation is requested, the content type will be "plain/text" to display the help text.

Otherwise, the results from the stored procedure are displayed:

```
[
    {
        "Number": 1,
        "Decimal": null
    }
]
```
## Sample Stored Procedure

The following stored procedure example can be used to create an endpoint.
* `@_User` is passed by the API of any authenticated user.
* `@_Claims` is passed by the API of any provided claims (array of JSON objects with attributes `Key` and `Value`).
* The API will only expose an API when an extended property with name "api" exists for the stored procedure.
* When the context "help" is added to a request, then the value of the extended property will be shown to document the endpoint.

```
IF OBJECT_ID('dbo.Test_Select') IS NULL
	EXEC sp_executesql N'CREATE PROC dbo.Test_Select AS SELECT 1'
GO

ALTER PROC dbo.Test_Select
	@Number INT = 0,
	@Decimal DECIMAL(38, 10) = NULL,
	@_User NVARCHAR(MAX) =  NULL,
	@_Claims NVARCHAR(MAX) = NULL
AS BEGIN
	SET NOCOUNT ON

	SELECT
		@Number AS Number,
		@Decimal AS [Decimal]
END
GO

DECLARE
	@Schema SYSNAME = 'dbo',
	@Object SYSNAME = 'Test_Select',
	@Docs SQL_VARIANT =
'Endpoint: dbo.Test_Select
	@Number INT -- The number to be displayed
	, @Decimal DECIMAL(38, 10) -- A decimal value to be displayed (optional)
	, @_User NVARCHAR(MAX) -- The name of the authenticated user
	, @_Claims NVARCHAR(MAX) -- The json array of key/value pairs of claim types and values
Return the following columns:
	Number - the number provided
	Decimal - the decimal value provided (optional input)
'
DECLARE @ObjectId INT = OBJECT_ID('[' + @Schema + '].[' + @Object + ']')
IF EXISTS (SELECT * FROM sys.extended_properties WHERE major_id = @ObjectId)
	EXEC sp_updateextendedproperty N'api', @Docs, 'Schema', @Schema, 'Procedure', @Object
ELSE
	EXEC sp_addextendedproperty N'api', @Docs, 'Schema', @Schema, 'Procedure', @Object
```