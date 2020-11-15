# ConnorWebSockets
https://www.nuget.org/packages/ConnorWebSockets/

## Why Use This Package
The goal of this package was to provide a definable class wrapper around the websocket to include information about the WebSocket and Connection. For example, to determine if that socket should have access to information that would be behind a layer of authentication. This also has the middleware and implementation to work with a .net core API that has other controllers that should not be impacted.

## How To Implement

### Create Your Socket Wrapper
You need to create your wrapper that will house the WebSocket and any information about it by inheriting the WebSocketBase class.
```
public class YourSocket : WebSocketBase
{
    public YourSocket(WebSocket socket) : base(socket)
    {
    }
}
```

### Create Your Socket Handler
This is the primary class you will be working in and will define what will happen during the lifecycle of your websocket wrapper defined above
```
public class YourHandler : WebSocketHandlerBase<YourSocket>
{
    public YourHandler(ConnectionManagerBase<YourSocket> manager, ILogger<YourHandler> logger) : base(manager, logger)
    {
    }
    
    public override async Task<string> OnConnected(YourSocket socket)
    {
        string socketId = await base.OnConnected(socket);
        // Do Stuff
        return socketId;
    }
    
    public override async Task OnDisconnected(YourSocket socket)
    {
        // Do Stuff
    }
    
    public override async Task ReceiveAsync(YourSocket socket, WebSocketReceiveResult result, byte[] buffer)
    {
        string socketId = WebSocketConnectionManager.GetId(socket);
        string message = Encoding.UTF8.GetString(buffer, 0, result.Count);        
        // Process Message
        string response = string.Empty;
        // Respond
        await SendMessageAsync(socketId, response);
    }
}
```

### Create Your Socket Middleware
This handles passing of the request to your socket handler to bring everything together, and can be implemented easily via
```
public class YourSocketMiddleware : SocketMiddleware<YourHandler, YourSocket>
{
    public YourSocketMiddleware(RequestDelegate next, YourHandler handler) : base(next, handler)
    {
    }
}
```

### Wire It Up
Now we need to handle the addition to the Startup.cs class
```
public void ConfigureServices(IServiceCollection services)
{
    // Your other stuff
    
    // Add Connection Managers
    services.AddTransient(typeof(ConnectionManagerBase<YourSocket>));
    
    // Add Handlers
    services.AddSingleton(typeof(YourHandler));
}
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    // Your other stuff
    var serviceScopeFactory = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>();
    var serviceProvider = serviceScopeFactory.CreateScope().ServiceProvider;
    app.UseWebSockets();
    app.UseEndpoints(endpoints => 
    {
        endpoints.MapControllers();
        var socketPipeline = endpoints.CreateApplicationBuilder().UseMiddleware<YourSocketMiddleware>(serviceProvider.GetService<YourHandler>()).Build();
        endpoints.Map("/yourpath", socketPipeline);
    });
}
```
