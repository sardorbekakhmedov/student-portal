using System.Text;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
var log = app.Logger;
log.LogInformation("Starting up,  DateTime:  {datetime}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

app.Use(async (context, next) =>
{
    context.Request.EnableBuffering();
    var bodyContent = "";
    if (context.Request.ContentLength > 0 && context.Request.Body.CanRead)
    {
        using var reader = new StreamReader(
            context.Request.Body,
            encoding: Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
             leaveOpen: true );
        
        bodyContent = await reader.ReadToEndAsync();
        context.Request.Body.Position = 0;
    }
    
    log.LogInformation("Request:  Datetime: {datetime}, Method: {method},  Path: {path}, Querystring: {querystring}, Body: {body}", 
        DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
        context.Request.Method,
        context.Request.Path, 
        string.IsNullOrWhiteSpace(context.Request.QueryString.Value) ? "empty" : context.Request.QueryString.Value,
        string.IsNullOrWhiteSpace(bodyContent) ? "empty" : bodyContent );
    
    await next();
    
    log.LogInformation("Response sent.");
});
app.Run();

