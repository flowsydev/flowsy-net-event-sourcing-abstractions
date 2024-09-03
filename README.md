# Flowsy Event Sourcing Abstractions

Event Sourcing is an architectural pattern in which changes to the state of an application are
stored as a sequence of events. Instead of storing just the current state of the data in a domain,
this pattern captures all changes as individual events. These events can be replayed to recreate
the system's state at any point in time.

This package provides basic abstractions to implement applications based on event sourcing concepts.

## IEvent

All the events defined by your application must implement the following interface:

```csharp
/// <summary>
/// Represents an event that occurred in the system.
/// </summary>
public interface IEvent
{
    /// <summary>
    /// The instant when the event occurred.
    /// </summary>
    DateTimeOffset OcurrenceInstant { get; }
}
```

To facilitate the implementation of such interface, we could define a DomainEvent abstract class and use it as the base class for all the events of our application.

```csharp
public abstract class DomainEvent : IEvent 
{  
    protected DomainEvent()
    {
        // The occurrence instant will always be the exact moment the event object is instantiated.
        // This can be useful to sort events chronologically even before they are persisted to the event store.
        OccurrenceInstant = DateTimeOffset.Now;
    }

    public DateTimeOffset OccurrenceInstant { get; }
}
```

Then we can define our application events by inheriting from the DomainEvent class.

```csharp
public abstract class ShoppingCartEvent : DomainEvent
{
    protected ShoppingCartEvent(string shoppingCartId)
    {
        ShoppingCartId = shoppingCartId;
    }
  
    public string ShoppingCartId { get; }
}

public sealed class ShoppingCartCreated : ShoppingCartEvent
{
    public ShoppingCartCreated(string shoppingCartId, string ownerUserId) : base(shoppingCartId)
    {
        OwnerUserId = ownerUserId;
    }
  
    public string OwnerUserId { get; }
}

public sealed class ShoppingCartItemAdded : ShoppingCartEvent
{
    public ShoppingCartItemAdded(
        string shoppingCartId,
        string shoppingCartItemId,
        string productId,
        decimal productPrice,
        double quantity
        ) : base(shoppingCartId)
    {
        ShoppingCartItemId = shoppingCartItemId;
        ProductId = productId;
        ProductPrice = productPrice;
        Quantity = quantity;
    }

    public string ShoppingCartItemId { get; }
    public string ProductId { get; }
    public decimal ProductPrice { get; }
    public double Quantity { get; }
    public decimal TotalPrice => ProductPrice * (decimal) Quantity;
}

public sealed class ShoppingCartItemRemoved : ShoppingCartEvent
{
    public ShoppingCartItemRemoved(string shoppingCartId, string shoppingCartItemId) : base(shoppingCartId)
    {
        ShoppingCartItemId = shoppingCartItemId;
    }
  
    public string ShoppingCartItemId { get; }
}

// Define more events
// ...

public sealed class ShoppingCartOrderPlaced : ShoppingCartEvent
{
    public ShoppingCartOrderPlaced(string shoppingCartId, bool userIsPremium) : base(shoppingCartId)
    {
        UserIsPremium = userIsPremium;
    }
  
    public bool UserIsPremium { get; }
}
```

## Aggregates

An aggregate represents a cluster of domain objects that can be treated as a single unit for data changes.
An object inheriting from the **AggregateRoot** class is responsible for ensuring the consistency of changes
within the aggregate boundaries, as well as enforcing invariants.

```csharp
public sealed class ShoppingCart : AggregateRoot<ShoppingCartEvent>
{
    public string OwnerUserId { get; private set; }
    public bool OwnerUserIsPremium { get; private set; }
  
    // List of fictitious ShoppingCartItem objects belonging to this shopping cart
    private readonly List<ShoppingCartItem> _items = [];
    public IEnumerable<ShoppingCartItem> Items => _items;
  
    public ShoppingCartStatus Status { get; private set; }
    public decimal Total => _items.Sum(item => item.TotalPrice);
    public decimal Discount { get; private set; }
    public decimal GrandTotal => Total - Discount;
  
    // Override the Apply method to provide the required actions for each type of event
    protected override void Apply(ShoppingCartEvent @event)
    {
        switch (@event)
        {
            case ShoppingCartCreated e:
                // The AggregateRoot base class implements the Id property of type string defined in IAggregateRoot.    
                // A convinient value for this property would be the shopping cart ID, so all
                // the events related to a single shopping cart can be grouped using this identifer.
                Id = e.ShoppingCartId;
                OwnerUserId = e.UserId;
                OwnerUserIsPremium = false;
                Discount = 0m;
                break;
  
            case ShoppingCartItemAdded e:
                {
                    var item = _items.FirstOrDefault(item => item.ShoppingCartItemId == e.ShoppingCartItemId);
                    if (item is null)
                    {
                        _items.Add(new ShoppingCartItem
                        {
                            ShoppingCartItemId = e.ShoppingCartItemId,
                            ProductId = e.ProductId,
                            ProductName = e.ProductName,
                            ProductPrice = e.ProductPrice,
                            Quantity = e.Quantity
                        });
                    }
                    else
                    {
                        item.Update(
                            e.ProductName,
                            e.ProductStock,
                            e.ProductPrice,
                            e.Quantity
                            ); 
                    }
                    Status = _items.Any() ? ShoppingCartStatus.Active : ShoppingCartStatus.Empty;
                }
                break;
  
            case ShoppingCartItemRemoved e:
                {
                    var item = _items.First(i => i.ShoppingCartItemId == e.ShoppingCartItemId);
                  
                    item.Decrease();
                    if (item.Quantity == 0)
                        _items.Remove(item);
                  
                    Status = _items.Any() ? ShoppingCartStatus.Active : ShoppingCartStatus.Empty;
                }
                break;
  
            // Apply other events
            // ...
  
            case ShoppingCartOrderPlaced e:
                UserIsPremium = e.UserIsPremium;    
    
                if (UserIsPremium)
                    Discount = Total * 0.15m;
    
                Status = ShoppingCartStatus.OrderPlaced;    
                break;
  
            default:
                throw new NotSupportedException($"Event not supported: {@event.GetType().Name}");
        }
    }
  
    public void Create(string shoppingCartId, User ownerUser)
    {
        ApplyChange(new ShoppingCartCreated(shoppingCartId, User.OwnerUserId));
        IsNew = true;
    }
  
    public void AddItem(string shoppingCartItemId, Product product, double quantity)
    {
        if (itemAdded.Quantity <= 0)
            throw new InvalidShoppingCartQuantityException("Item quantity must be a positive number.");
  
        var item = _items.FirstOrDefault(item => item.ShoppingCartItemId == shoppingCartItemId);
        var newQuantity = (item?.Quantity ?? 0) + quantity;
  
        // Product stock validation was placed here for demonstration purposes only,
        // in real scenarios, product stock availability is usually verified on checkout.  
        if (newQuantity > product.Stock)
            throw new ProductStockValidationException("Item quantity exceeds the current product stock."));
  
        ApplyChange(new ShoppingCartItemAdded(
            shoppingCartItemId,
            product.ProductId,
            product.Name,
            product.Price,
            quantity
            ));
    }
  
    public void RemoveItem(string shoppingCartItemId)
    {
        var item = _items.FirstOrDefault(i => i.ShoppingCartItem == shoppingCartItemId);
        if (item is null)
            throw new EntityNotFoundException($"Order item not found: {shoppingCartItemId}.")
  
        ApplyChange(new ShoppingCartItemRemoved(shoppingCartItemId));
    }
  
    public void PlaceOrder(bool userIsPremium)
    {
        if (!_items.Any())
            throw new EmptyShoppingCartException("The shopping cart is empty.");
  
        ApplyChange(new ShoppingCartOrderPlaced(user.IsPremium));
    }
  
    // Add more methods to model business behavior
}
```

## Aggregate Root & Application Commands

It's a common practice to implement the CQRS pattern to perform actions on our aggregate roots.
The following examples demonstrate how to use the ShoppingCart aggregate root to create and manage the user's shopping cart.

### The Aggregate Repository

This package includes the **IAggregateRepository** interface with methods to save and load events from some event store.

We can define interfaces for the repositories of our domain objects and use them to manage the persistence of our aggregates.

```csharp
// Interface for the shopping cart repository used to manage the persistence of shopping cart aggregates. 
public interface IShoppingCartRepository : IAggregateRepository<ShoppingCart, ShoppingCartEvent>
{
}

// Interface for the product repository used to manage the persistence of product aggregates.
public interface IProductRepository : IAggregateRepository<Product, ProductEvent>
{
}

// Interface for the user repository used to manage the persistence of user aggregates.
public interface IUserRepository : IAggregateRepository<User, UserEvent>
{
}
```

**Note:** The implementation of this interface is beyond the scope of this library, but you can take a look
at the [Flowsy.EventSourcing.Sql](https://www.nuget.org/packages/Flowsy.EventSourcing.Sql) package,
which includes an implementation based on SQL databases and JSON serialization as the underlying event store.

### Create a Shopping Cart

```csharp
public class CreateShoppingCartCommand
{
    public CreateShoppingCartCommand(string shoppingCartId, string ownerUserId)
    {
        ShoppingCartId = shoppingCartId;
        OwnerUserId = ownerUserId;
    }
  
    public string ShoppingCartId { get; }
    public string OwnerUserId { get; }
}

public class CreateShoppingCartCommandHandler
{
    private readonly IShoppingCartRepository _shoppingCartRepository;
    private readonly IUserRepository _userRepository;
  
    public CreateShoppingCartCommandHandler(
        IShoppingCartRepository eventRepository,
        IUserRepository userRepository
        )
    {
        _shoppingCartRepository = eventRepository;
        _userRepository = userRepository;
    }
  
    public async Task HandleAsync(CreateShoppingCartCommand command, CancellationToken cancellationToken)
    {
        var user = await _userRepository.LoadAsync(command.UserId, cancellationToken);
        if (user is null)
            throw new EntityNotFoundException($"User not found: {command.UserId}");
        
        var shoppingCart = new ShoppingCart();    
        shoppingCart.Create(command.ShoppingCartId, user);
    
        await _shoppingCartRepository.SaveAsync(shoppingCart, cancellationToken);
    }
}
```

### Add Items to a Shopping Cart

```csharp
public class AddShoppingCartItemCommand
{
    public AddShoppingCartItemCommand(
        string shoppingCartId,
        string shoppingCartItemId,
        string productId
        double quantity
        )
    {
        ShoppingCartId = shoppingCartId;
        ShoppingCartItemId = shoppingCartItemId;
        ProductId = productId;
        Quantity = quantity;
    }
  
    public string ShoppingCartId { get; }
    public string ShoppingCartItemId { get; }
    public string ProductId { get; }
    public double Quantity { get; }
}

public class AddShoppingCartItemCommandHandler
{
    private readonly IShoppingCartRepository _shoppingCartRepository;
    private readonly IProductRepository _productRepository;
  
    public AddShoppingCartItemCommandHandler(
        IShoppingCartRepository eventRepository,
        IProductRepository productRepository
        )
    {
        _shoppingCartRepository = eventRepository;
        _productRepository = productRepository;
    }
  
    public async Task HandleAsync(AddShoppingCartItemCommand command, CancellationToken cancellationToken)
    {
        var shoppingCart = await _shoppingCartRepository.LoadAsync(command.ShoppingCartId, cancellationToken);
        if (shoppingCart is null)
            throw new EntityNotFoundException($"Shopping cart not found: {command.ShoppingCartId}");
    
        var product = await _productRepository.LoadAsync(command.ProductId, cancellationToken);
        if (product is null)
            throw new EntityNotFoundException($"Product not found: {command.ProductId}");
    
        shoppingCart.AddItem(command.ShoppingCartItemId, product, command.Quantity);
    
        await _shoppingCartRepository.SaveAsync(shoppingCart, cancellationToken);
    }
}
```

### Remove Items from a Shopping Cart

```csharp
public class RemoveShoppingCartItemCommand
{
    public RemoveShoppingCartItemCommand(
        string shoppingCartId,
        string shoppingCartItemId
        )
    {
        ShoppingCartId = shoppingCartId;
        ShoppingCartItemId = shoppingCartItemId;
    }
  
    public string ShoppingCartId { get; }
    public string ShoppingCartItemId { get; }
}

public class RemoveShoppingCartItemCommandHandler
{
    private readonly IShoppingCartRepository _shoppingCartRepository;
  
    public RemoveShoppingCartItemCommandHandler(IShoppingCartRepository eventRepository)
    {
        _shoppingCartRepository = eventRepository;
    }
  
    public async Task HandleAsync(RemoveShoppingCartItemCommand command, CancellationToken cancellationToken)
    {
        var shoppingCart = await _shoppingCartRepository.LoadAsync(command.ShoppingCartId, cancellationToken);
        if (shoppingCart is null)
            throw new EntityNotFoundException($"Shopping cart not found: {command.ShoppingCartId}");
    
        shoppingCart.RemoveItem(command.ShoppingCartItemId);
    
        await _shoppingCartRepository.SaveAsync(shoppingCart, cancellationToken);
    }
}
```

### Place the Order

```csharp
public class PlaceShoppingCartOrderCommand
{
    public PlaceShoppingCartOrderCommand(
        string shoppingCartId,
        string cardNumber,
        string cardHolder,
        int cardExpirationYear,
        int cardExpirationMonth,
        string cardSecurityCode
        )
    {
        ShoppingCartId = shoppingCartId;
        CardNumber = cardNumber;
        CardHolder = cardHolder;
        CardExpirationYear = cardExpirationYear;
        CardExpirationMonth = cardExpirationMonth;
        CardSecurityCode = cardSecurityCode;
    }
  
    public string ShoppingCartId { get; }
    public string CardNumber { get; }
    public string CardHolder { get; }
    public int CardExpirationYear { get; }
    public int CardExpirationMonth { get; }
    public string CardSecurityCode { get; }
}

public class PlaceShoppingCartOrderCommandHandler
{
    private readonly IShoppingCartRepository _shoppingCartRepository;
    private readonly IUserRepository _userRepository;
    private readonly IPaymentService _paymentService;
  
    public PlaceShoppingCartOrderCommandHandler(
        IShoppingCartRepository shoppingRepository;,
        IUserRepository userRepository;,
        IPaymentService paymentService
        )
    {
        _shoppingCartRepository = shoppingCartRepository;
        _userRepository = userRepository;
        _paymentService = paymentService;
    }
  
    public async Task HandleAsync(PlaceShoppingCartOrderCommand command, CancellationToken cancellationToken)
    {
        var shoppingCart = await _shoppingCartRepository.LoadAsync(command.ShoppingCartId, cancellationToken);
        if (shoppingCart is null)
            throw new EntityNotFoundException($"Shopping cart not found: {command.ShoppingCartId}");
    
        // Load user data
        var ownerUser = await _userRepository.LoadAsync(shoppingCart.OwnerUserId, cancellationToken);
        if (ownerUser is null)
            throw new EntityNotFoundException($"User not found: {shoppingCart.OwnerUserId}");
    
        // Change the shopping cart state to 'OrderPlaced'
        shoppingCart.PlaceOrder(ownerUser.IsPremium);
    
        // Process payment
        await paymentService.ChargeAsync(
            shoppingCart,
            command.CardNumber,
            command.CardHolder,
            command.CardExpirationYear,
            command.CardExpirationMonth,
            command.CardSecurityCode,
            cancellationToken
            );
    
        // Persist the shopping cart events only if the payment was processed successfully
        await _shoppingCartRepository.SaveAsync(shoppingCart, cancellationToken);
    }
}
```

## Publishing Events

By implementing the **IEventPublisher** interface we can notify another components of our application when something relevant occurs.

The **DbAggregateRepository** class from the [Flowsy.EventSourcing.Sql](https://www.nuget.org/packages/Flowsy.EventSourcing.Sql) package
implements the **IAggregateRepository** interface defined in this package and accepts an optional **IEventPublisher** instance through
its constructor, so it can automatically publish the events of an **AggregateRoot** once they are saved to the event store.

### The Abstract Event Publisher

This package includes an abstract class named **EventPublisher** that implements the **IEventPublisher** interface using virtual and abstract methods,
so you can override them to customize the behavior of event publishing for each aggregate of your application.

For instance you could unify the process of event publishing for all the aggregates of your application by creating a class that inherits from **EventPublisher**,
implementing the **PublishAsync** method to publish the events using the [MediatR](https://www.nuget.org/packages/MediatR) library and then use this class as the base class for all the event publishers of your application.

First, we need to update our **DomainEvent** class to implement the **INotification** interface from the MediatR library.

```csharp
public abstract class DomainEvent : IEvent, INotification
{  
    protected DomainEvent()
    {
        // The occurrence instant will always be the exact moment the event object is instantiated.
        // This can be useful to sort events chronologically even before they are persisted to the event store.
        OccurrenceInstant = DateTimeOffset.Now;
    }

    public DateTimeOffset OccurrenceInstant { get; }
}
```

```csharp
public abstract class DomainEventPublisher<TAggregateRoot, TEventBase> : EventPublisher<TAggregateRoot, TEventBase>
    where TAggregateRoot : AggregateRoot<TEventBase>
    where TEventBase : DomainEvent, IEvent
{
    private readonly IPublisher _mediatorPublisher;

    protected DomainEventPublisher(IPublisher mediatorPublisher)
    {
        _mediatorPublisher = mediatorPublisher;
    }
  
    // The EventPublisher class only requires you to implement two methods:
  
    public override Task PublishAsync(IEnumerable<TEventBase> events, CancellationToken cancellationToken)
    {
        foreach (var notification in events.Where(e => e is INotification).Cast<INotification>())
            await _mediatorPublisher.Publish(notification, cancellationToken);
    }
  
    public override void PublishAndForget(IEnumerable<TEventBase> events)
    {
        Task.Run(() =>
        {
            try
            {
                PublishAsync(events, CancellationToken.None).Wait();
            }
            catch (Exception exception)
            {
                // Handle the exception
            }
        });
    }
}
```

```csharp
public sealed class ShoppingCartEventPublisher : DomainEventPublisher<ShoppingCart, ShoppingCartEvent>
{
    public ShoppingCartEventPublisher(IPublisher mediatorPublisher) : base(mediatorPublisher)
    {
    }
}

public sealed class ProductEventPublisher : DomainEventPublisher<Product, ProductEvent>
{
    public ProductEventPublisher(IPublisher mediatorPublisher) : base(mediatorPublisher)
    {
    }
}
```

That's it! Just by inheriting from your **DomainEventPublisher** class, any other event publisher in your application will automatically publish the events using the MediatR library.

## Important Note

The previous examples were written only to show how to use the abstractions included in this package,
in real applications, there are more elements to consider, such as distributed transactions,
error handling, logging and so on.
