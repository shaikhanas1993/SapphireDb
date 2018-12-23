# Realtime Database - Server Configuration

## Installation

### Install Package
To use the realtime database on server side your first need to install the nuget package.

In an Asp.Net Core project execute

```
PM > Install-Package RealtimeDatabase
```

https://www.nuget.org/packages/RealtimeDatabase/

### Configure DbContext

Create a new context or use an existing and derive from RealtimeDbContext

```csharp
// Change DbContext to RealtimeDbContext
public class MyDbContext : RealtimeDbContext
{
    //Add RealtimeDatabaseNotifier for DI
    public RealtimeContext(DbContextOptions<RealtimeContext> options, RealtimeDatabaseNotifier notifier) : base(options, notifier)
    {

    }

    public DbSet<User> Users { get; set; }

    public DbSet<Test> Tests { get; set; }
    
    ...
}
```

### Register the realtime database service

In the service configuration (normally in Startup.cs) add your DbContext and RealtimeDatabase

```csharp
public void ConfigureServices(IServiceCollection services)
{
    ...
    // Register services
    services.AddRealtimeDatabase<MyDbContext>();
    services.AddDbContext<MyDbContext>(cfg => ...));
    ...
}
```

### Configure Request Pipeline

Add RealtimeDatabase in your pipeline configuration

```csharp

public void Configure(IApplicationBuilder app, IHostingEnvironment env)
{
    ...
    //Add Middleware
    app.UseRealtimeDatabase();
}
```

When using Authentication make sure to call it before RealtimeDatabase

## Configuration

### Make Entity Properties Updatable

To make properties of an Entity updatable using the update method of the realtime collection
you have to add the `UpdatableAttribute` to the class or properties of it. All other properties
cannot be changed using the realtime methods at client side.

Make all properties of the class updatable:
```csharp
[Updatable]
public class User : Base
{
    [Required]
    [MinLength(3)]
    public string Username { get; set; }

    [Required]
    [MinLength(3)]
    public string FirstName { get; set; }

    [Required]
    [MinLength(3)]
    public string LastName { get; set; }
}
```

Only make Username updatable:
```csharp
public class User : Base
{
    [Updatable]
    [Required]
    [MinLength(3)]
    public string Username { get; set; }

    [Required]
    [MinLength(3)]
    public string FirstName { get; set; }

    [Required]
    [MinLength(3)]
    public string LastName { get; set; }
}
```

### Authentication/Authorization

You can protect specific actions on entity classes by using the attributes
`QueryAuthAttribute`, `CreateAuthAttribute`, `UpdateAuthAttribute` and `RemoveAuthAttribute`.

If you just use the plain attributes without any configuration they will just enable authentication
for the specific action and model.

For example:
```csharp
[QueryAuth]     // Will require an authenticated request to allow query users
[RemoveAuth]    // Will require an authenticated request to allow remove users
                // All other operations are allowed without authentication
public class User : Base
{
    [Required]
    [MinLength(3)]
    public string Username { get; set; }

    [Required]
    [MinLength(3)]
    public string FirstName { get; set; }

    [Required]
    [MinLength(3)]
    public string LastName { get; set; }
}
```

You can also define roles that are authorized to perform a specific action:
```csharp
[QueryAuth]             // Will require an authenticated request to allow query
[RemoveAuth("admin")]   // Will require an authenticated request and role 
                        // 'admin' to allow remove
public class User : Base
{
    [Required]
    [MinLength(3)]
    public string Username { get; set; }

    [Required]
    [MinLength(3)]
    public string FirstName { get; set; }

    [Required]
    [MinLength(3)]
    public string LastName { get; set; }
}
```


The `QueryAuthAttribute` and `UpdateAuthAttribute` can also be used for properties.
You can use it to control query or update of a specific property.
If a property is not queryable beacause the user is not authorized to it is just omitted
and does not get transmitted to the client. The same behavior is used when
an update of a property is not allowed for a user: The property just is omitted and not changed.

```csharp
[QueryAuth]
public class User : Base
{
    [Required]
    [MinLength(3)]
    public string Username { get; set; }

    [Required]
    [MinLength(3)]
    [QueryAuth("admin")]        // Property FirstName can only get queried by 
                                // users with role `admin`
    public string FirstName { get; set; }

    [Required]
    [MinLength(3)]
    [Updatable]
    [UpdateAuth("admin")]       // LastName can only get updated by users with role
                                // `admin`
    public string LastName { get; set; }
}
```


## Realtime Auth

Realtime database comes with a JWT Provider. The client can call the methods for login etc. over websocket.
 
You only have to add this to your appsettings.json:

```
{
  "JwtOptions": {
    "Issuer": "RealtimeDatabase",
    "SecretKey": "secret",
    "ValidFor": 60
  }
}
```

You have to enable the provider using:

```
services.AddRealtimeAuth<RealtimeAuthContext<AppUser>, AppUser>(
    new JwtOptions(Configuration.GetSection(nameof(JwtOptions))),
    cfg => cfg.UseFileContext(databasename: "auth"));
```

Also add this to your request pipeline:

```
app.UseRealtimeAuth();
```

Now the client can login using realtime database authentication.


## Actions

You can define custom actions at server side that can be called
by the client.

### Create ActionHandler

First of all create a class that acts as your action handler.
Here you can define custom methods as you like. You can use
all primitive types as parameters. (string, int etc.)
Realtime database automatically discovers all handlers that end with `Actions`.

The client can call the camel cased name of the action and the handler.

````
public class ExampleActions : ActionHandlerBase
{
    public void Test()
    {
        Console.WriteLine("This is a test");
    }
}
````

The name would be: example.test

You can also use dependency injection:

````
public class ExampleActions : ActionHandlerBase
{
    private readonly RealtimeContext db;

    public ExampleActions(RealtimeContext _db)
    {
        db = _db;
    }
}
````

### Send notifications

To send notifications during the execution of an action use `Notify()`.

Example:

````
public int GenerateRandomNumber()
{
    for (int i = 0; i <= 100; i++)
    {
        Thread.Sleep(10);
        Notify("Progress: " + i + "%");
    }

    return db.Users.Count();
}
````

### Authentication/Authorization

You can secure a handler or a single action using the `ActionAuthAttribute`.
Just add it to your handler or method:

````
[ActionAuth]
public class ExampleActions : ActionHandlerBase
{
    private readonly RealtimeContext db;

    public ExampleActions(RealtimeContext _db)
    {
        db = _db;
    }

    [ActionAuth("admin", "user")]
    public int GenerateRandomNumber()
    {
        for (int i = 0; i <= 100; i++)
        {
            Thread.Sleep(10);
            Notify("Progress: " + i + "%");
        }

        return db.Users.Count();
    }

    public string TestWithParams(string param1, string param2)
    {
        return param1 + param2;
    }

    public void NoReturn()
    {
        Console.WriteLine("This is a test");
    }
}
````

## Messaging

Realtime database also brings features for commincation with the client.
You can send messages to the client and the client also supports a publish/subscribe pattern.

You can send messages to the client using the `RealtimeMessageSender`.

Just use DI to get it:
```
public MessageActions(RealtimeMessageSender messageSender)
{

}
```

### Send

Using the method `Send(message)` you can send a message to all client.

You can also filter the clients using a Lambda:
```
MessageSender.Send(c => c.HttpContext.User.IsInRole("admin"), message);
```

### Publish

Using the method `Publish(topic, message)` you can publish a message to a specific topic.
Only client subscribing this topic will get the message:

```
MessageSender.Publish("test", "test Message");
```