using System.Text;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
var log = app.Logger;
log.LogInformation("Starting up,  DateTime:  {datetime}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
var fullUrl = "";

app.Use(async (context, next) =>
{
    context.Request.EnableBuffering();
    var bodyContent = "";
    if (context.Request is { ContentLength: > 0, Body.CanRead: true })
    {
        using var reader = new StreamReader(
            context.Request.Body,
            encoding: Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
             leaveOpen: true );
        
        bodyContent = await reader.ReadToEndAsync();
        context.Request.Body.Position = 0;
    }
    
    fullUrl = $"{context.Request.Scheme}://{context.Request.Host}{context.Request.PathBase}{context.Request.Path}{context.Request.QueryString}";
    log.LogInformation("Request:  Datetime: {datetime}, Method: {method},  Path: {path}, Querystring: {querystring}, Body: {body}", 
        DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
        context.Request.Method,
        fullUrl, 
        string.IsNullOrWhiteSpace(context.Request.QueryString.Value) ? "empty" : context.Request.QueryString.Value,
        string.IsNullOrWhiteSpace(bodyContent) ? "empty" : bodyContent );
    
    await next();
    log.LogInformation("Response sent.");
});

app.UseWhen(
    context => context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase),
    configuration: branch =>
    {
        branch.Use(async (context, next) =>
        {
            log.LogInformation("API Request detected");
            await next();
        });
    });

app.Map(
    pathMatch: "/students", 
    configuration: contextApp =>
{
    contextApp.Use(async (context, next) =>
    {
        log.LogInformation(" Inside Students branch");
        await next();
    });
    
    contextApp.Run( async context =>
    {
        context.Response.ContentType = "text/plain; charset=utf-8";
        await context.Response.WriteAsync("Student Management Section");
    });
});

app.Map(
pathMatch: "/teachers", 
configuration: contextApp =>
{
    contextApp.Use(async (context, next) =>
    {
        log.LogInformation(" Inside Teachers branch");
        await next();
    });
    
    contextApp.Run( async context =>
    {
        context.Response.ContentType = "text/plain; charset=utf-8";
        await context.Response.WriteAsync("Teacher Management Section");
    });
});

app.MapWhen(
    context => context.Request.Query.ContainsKey("admin"),
    configuration: adminApp => 
    {
        adminApp.Run(async context =>
        {
            var adminValue = context.Request.Query["admin"].ToString();
            if (string.Equals(adminValue, "true", StringComparison.OrdinalIgnoreCase))
            {
                log.LogInformation("Admin Dashboard");
                context.Response.ContentType = "text/plain; charset=utf-8";
                await context.Response.WriteAsync("Admin Dashboard Section");
            }
            else
            {
                log.LogWarning("Invalid admin query value: {value}", adminValue);
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                context.Response.ContentType = "text/plain; charset=utf-8";
                await context.Response.WriteAsync("Incorrect format for 'admin' query parameter. Expected ?admin=true");
            }
        });
    });

app.Run(async context =>
{
    if (context.Request.Path == "/" || string.IsNullOrEmpty(context.Request.Path.Value))
    {
        log.LogInformation("Home page");
        context.Response.ContentType = "text/plain; charset=utf-8";
        await context.Response.WriteAsync("Welcome to the Student Portal");
        return;
    }
    
    log.LogInformation("Unknown route inside root map");
    context.Response.StatusCode = StatusCodes.Status404NotFound;
    context.Response.ContentType = "text/plain; charset=utf-8";
    await context.Response.WriteAsync("Page Not Found!");
});

app.Run();


/*1. Request `GET /` → Response: `Welcome to the Student Portal`
2. Request `GET /students` → Logs `"Inside Students branch"`, Response: `Student Management Section`
3. Request `GET /teachers` → Response: `Teacher Management Section`
4. Request `GET /anything` → Response: `Page Not Found`
5. Request `GET /api/data` → Logs `"API Request detected"`, Response: `Page Not Found` (since no specific handler)
6. Request `GET /?admin=true` → Response: `Admin Dashboard`*/
