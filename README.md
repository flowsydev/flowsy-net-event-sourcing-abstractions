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
/// Represents a domain event.
/// </summary>
public interface IEvent
{
}
```

For example, we can define a ShoppingCartEvent abstract class and use it as the
base class for all the events related to a given shopping cart.

```csharp
public abstract class ShoppingCartEvent : IEvent
{
    protected ShoppingCartEvent()
    {
    }
}

public sealed class ShoppingCartCreated : ShoppingCartEvent
{
    public ShoppingCartCreated(Guid shoppingCartId, Guid userId)
    {
        ShoppingCartId = shoppingCartId;
        UserId = userId;
    }

    public Guid ShoppingCartId { get; }
    public Guid UserId { get; }
}

public sealed class ShoppingCartItemAdded : ShoppingCartEvent
{
    public ShoppingCartItemAdded(
        Guid shoppingCartItemId,
        Guid productId,
        double productStock,
        decimal productPrice,
        double quantity
        )
    {
        ShoppingCartItemId = shoppingCartItemId;
        ProductId = productId;
        ProductStock = productStock;
        ProductPrice = productPrice;
        Quantity = quantity;
    }

    public Guid ShoppingCartItemId { get; }
    public Guid ProductId { get; }
    public double ProductStock { get; }
    public decimal ProductPrice { get; }
    public double Quantity { get; }
    public decimal TotalPrice => ProductPrice * (decimal) Quantity;
}

public sealed class ShoppingCartItemRemoved : ShoppingCartEvent
{
    public ShoppingCartItemRemoved(Guid shoppingCartItemId)
    {
        ShoppingCartItemId = shoppingCartItemId;
    }
  
    public Guid ShoppingCartItemId { get; }
}

// Define more events
// ...

public sealed class ShoppingCartOrderPlaced : ShoppingCartEvent
{
    public ShoppingCartOrderPlaced(bool userIsPremium)
    {
        UserIsPremium = userIsPremium;
    }
    
    public bool UserIsPremium { get; }
}
```

## Aggregate Root

In Domain-Driven Design an aggregate root represents the entry point to a cluster of domain
objects (aggregate) treated as a single unit.
An aggregate root ensures the consistency of changes being made within the aggregate boundary
and enforces invariants.

It's important to note that event sourcing requires separation of write and read operations.
The write operations shall occur within the boundaries of aggregate roots, so every event applied to an
aggregate root is persisted as a single record and produce a side effect in the read model through projections.

For example, the event of adding an item to a shopping cart could affect two read models of a SQL database:
the shopping_cart table to update totals and the shopping_cart_item table to insert a new record.

Having said that, our application can be composed by namespaces that organize our entities and projections:

* Company.Project.Sales.Domain.Entities.ShoppingCart: Aggregate root that handles shopping cart actions.
* Company.Project.Sales.Domain.Entities.ShoppingCartItem: Entity managed by the ShoppingCart aggregate root to keep track of products in a shopping cart.
* Company.Project.Sales.Domain.Entities.Product: Aggregate root that manages product information and pricing.
* Company.Project.Sales.Domain.Projections.ShoppingCart: Read model used to browse a list of shopping carts.
* Company.Project.Sales.Domain.Projections.ShoppingCartItem: Read model used to browse the items of a give shopping cart.
* Company.Project.Sales.Domain.Projections.Product: Read model used to browse a list of products.

As you can see, you can have more than one class with the same name, but organized in different namespaces.
If you want to, you can also use different names or add suffixes to avoid confusion, for example:
* Company.Project.Sales.Domain.Entities.Product
* Company.Project.Sales.Domain.Projections.ProductProjection (ProductReadModel, ProductDto or whatever convention you prefer)

The important thing is to define your own conventions and to be consistent throughout your application.

The **AggregateRoot** abstract class in this package provides basic functionality to validate and apply events
to a given aggregate root required by your application.

```csharp
public sealed class ShoppingCart : AggregateRoot<ShoppingCartEvent>
{
    public Guid UserId { get; private set; }
    public bool UserIsPremium { get; private set; }
    
    // List of fictitious ShoppingCartItem objects belonging to this shopping cart
    private readonly List<ShoppingCartItem> _items = new ();
    public IEnumerable<ShoppingCartItem> Items => _items;
    
    public ShoppingCartState State { get; private set; }
    public decimal Total => _items.Sum(item => item.TotalPrice);
    public decimal Discount { get; private set; }
    public decimal GrandTotal => Total - Discount;
  
    // In this example, the IShoppingCartEventPublisher interface should inherit from IEventPublisher<TEvent>
    // and its implementation should notify other components of your application when relevant events occur.
    public ShoppingCart(IShoppingCartEventPublisher? shoppingCartEventPublisher) 
        : base(shoppingCartEventPublisher)
    {
    }
  
    // Override the Validate method to provide the required validations for each type of event
    protected override EventValidationResult<ShoppingCartEvent> Validate(ShoppingCartEvent @event)
    {
        string? message = null;
        var errors = new List<EventValidationError>();
    
        switch (@event)
        {
            case ShoppingCartItemAdded itemAdded:
                if (itemAdded.Quantity <= 0)
                    errors.Add(new EventValidationError("Item quantity must be a positive number."));
                
                // Product stock validation was placed here for demonstration purposes only,
                // in real scenarios, product stock availability is usually verified on checkout.
                
                var item = _items.FirstOrDefault(item => item.ShoppingCartItemId == itemAdded.ShoppingCartItemId);
                var newQuantity = (item?.Quantity ?? 0) + itemAdded.Quantity;                
                
                if (newQuantity > itemAdded.ProductStock)
                    errors.Add(new EventValidationError("Item quantity exceeds the current product stock."));
            
                if (errors.Any())
                    message = "Cannot add a new item to the shopping cart.";
                
                break;
        
            case ShoppingCartItemRemoved itemRemoved:
                var item = _items.FirstOrDefault(i => i.ShoppingCartItem == itemRemoved.ShoppingCartItemId);
                if (item is null)
                {
                    errors.Add(new EventValidationError($"Order item not found: {itemRemoved.ShoppingCartItemId}."));
                    message = "Cannot remove item from the current order.";
                }
                
                break;
            
            case ShoppingCartOrderPlaced orderPlaced:
                if (!_items.Any())
                {
                    errors.Add(new EventValidationError("The shopping cart is empty."));
                    message = "Cannot place order.";
                }
                break;
        
            // Validate other events
            // ...
        }

        return new EventValidationResult<ShoppingCartEvent>(@event, message, errors);
    }
  
    // Override the Apply method to provide the required actions for each type of event
    protected override void Apply(ShoppingCartEvent @event)
    {
        switch (@event)
        {
            case ShoppingCartCreated cartCreated:
                // The AggregateRoot base class defines a Key property of type string.                
                // A convinient value for this property would be the shopping cart ID, so all
                // the events related to a single shopping cart can be grouped using this key.
                Key = cartCreated.ShoppingCartId.ToString();
                UserId = cartCreated.UserId;
                UserIsPremium = false;
                Discount = 0m;
                break;
        
            case ShoppingCartItemAdded itemAdded:
                var item = _items.FirstOrDefault(item => item.ShoppingCartItemId == itemAdded.ShoppingCartItemId);
                if (item is null)
                {
                    _items.Add(new ShoppingCartItem
                    {
                        ShoppingCartItemId = itemAdded.ShoppingCartItemId,
                        ProductId = itemAdded.ProductId,
                        ProductName = itemAdded.ProductName,
                        ProductStock = itemAdded.ProductStock,
                        ProductPrice = itemAdded.ProductPrice,
                        Quantity = itemAdded.Quantity
                    });
                }
                else
                {
                    item.Update(
                        itemAdded.ProductName,
                        itemAdded.ProductStock,
                        itemAdded.ProductPrice,
                        itemAdded.Quantity
                        ); 
                }
                State = _items.Any() ? ShoppingCartState.Active : ShoppingCartState.Empty;
                break;
        
            case ShoppingCartItemRemoved itemRemoved:
                var item = _items.Find(i => i.ShoppingCartItemId == itemRemoved.ShoppingCartItemId);
                _items.Remove(item);
                State = _items.Any() ? ShoppingCartState.Active : ShoppingCartState.Empty;
                break;
        
            // Apply other events
            // ...
            
            case ShoppingCartOrderPlaced orderPlaced:
                UserIsPremium = orderPlaced.UserIsPremium;                
                
                if (UserIsPremium)
                    Discount = Total * 0.15m;
                
                State = ShoppingCartState.OrderPlaced;                
                break;
        
            default:
                throw new NotSupportedException($"Event not supported: {@event.GetType().Name}");
        }
    }
  
    public void Create(Guid shoppingCartId, User user)
    {
        ApplyChange(new ShoppingCartCreated(shoppingCartId, User.UserId));
    }
  
    public void AddItem(Guid shoppingCartItemId, Product product, double quantity)
    {
        ApplyChange(new ShoppingCartItemAdded(
            shoppingCartItemId,
            product.ProductId,
            product.Name,
            product.Stock,
            product.Price,
            quantity
            ));
    }
  
    public void RemoveItem(Guid shoppingCartItemId)
    {
        ApplyChange(new ShoppingCartItemRemoved(shoppingCartItemId));
    }
  
    public void PlaceOrder(bool userIsPremium)
    {
        ApplyChange(new ShoppingCartOrderPlaced(user.IsPremium));
    }
  
    // Add more methods to model business behavior
}
```


## Aggregate Root & Application Commands

It's a common practice to implement the CQRS pattern to perform actions on our aggregate roots.
The following examples demonstrate how to use the ShoppingCart aggregate root to
create and manage the user's shopping cart.

### The Event Store
Define your own interface derived from IEventStore\<TEvent> to define an event store for all
the events related to a shopping cart.

```csharp
public interface IShoppingCartEventStore : IEventStore<ShoppingCartEvent>
{
}
```

**Note:** The implementation of this interface is beyond the scope of this library, but you can take a look
at the [Flowsy.EventSourcing.Sql](https://www.nuget.org/packages/Flowsy.EventSourcing.Sql) package, where
you can find base classes to implement event stores powered by a SQL database.


### Create a Shopping Cart
```csharp
public class CreateShoppingCartCommand
{
    public CreateShoppingCartCommand(Guid shoppingCartId, Guid userId)
    {
        ShoppingCartId = shoppingCartId;
        UserId = userId;
    }
    
    public Guid ShoppingCartId { get; }
    public Guid UserId { get; }
}

public class CreateShoppingCartCommandHandler
{
    private readonly IShoppingCartEventStore _shoppingCartEventStore;
    private readonly IUserRepository _userRepository;
    
    public CreateShoppingCartCommandHandler(
        IShoppingCartEventStore shoppingCartEventStore,
        IUserRepository userRepository
        )
    {
        _shoppingCartEventStore = shoppingCartEventStore;
        _userRepository = userRepository;
    }
    
    public async Task HandleAsync(CreateShoppingCartCommand command, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null)
            throw new UserNotFoundException(userId);
            
        var shoppingCart = new ShoppingCart();
        
        shoppingCart.Create(command.ShoppingCartId, user);
        
        await _shoppingCart.SaveAsync(_shoppingCartEventStore, cancellationToken);
    }
}
``` 

### Add Items to a Shopping Cart
```csharp
public class AddShoppingCartItemCommand
{
    public AddShoppingCartItemCommand(
        Guid shoppingCartId,
        Guid shoppingCartItemId,
        Guid productId
        double quantity
        )
    {
        ShoppingCartId = shoppingCartId;
        ShoppingCartItemId = shoppingCartItemId;
        ProductId = productId;
        Quantity = quantity;
    }
    
    public Guid ShoppingCartId { get; }
    public Guid ShoppingCartItemId { get; }
    public Guid ProductId { get; }
    public double Quantity { get; }
}

public class AddShoppingCartItemCommandHandler
{
    private readonly IShoppingCartEventStore _shoppingCartEventStore;
    private readonly IProductRepository _productRepository;
    
    public AddShoppingCartItemCommandHandler(
        IShoppingCartEventStore shoppingCartEventStore,
        IProductRepository productRepository
        )
    {
        _shoppingCartEventStore = shoppingCartEventStore;
        _productRepository = productRepository;
    }
    
    public async Task HandleAsync(AddShoppingCartItemCommand command, CancellationToken cancellationToken)
    {
        var shoppingCart = new ShoppingCart();
        
        await shoppingCart.LoadAsync(
            command.ShoppingCartId.ToString(),
            _shoppingCartEventStore,
            cancellationToken
            );
        
        var product = await _productRepository.GetByIdAsync(command.ProductId, cancellationToken);
        if (product is null)
            throw new ProductNotFoundException(command.ProductId);
        
        shoppingCart.AddItem(
            command.ShoppingCartItemId,
            product,
            command.Quantity
            );
        
        await shoppingCart.SaveAsync(_shoppingCartEventStore, cancellationToken);
    }
}
```

### Remove Items from a Shopping Cart
```csharp
public class RemoveShoppingCartItemCommand
{
    public RemoveShoppingCartItemCommand(
        Guid shoppingCartId,
        Guid shoppingCartItemId
        )
    {
        ShoppingCartId = shoppingCartId;
        ShoppingCartItemId = shoppingCartItemId;
    }
    
    public Guid ShoppingCartId { get; }
    public Guid ShoppingCartItemId { get; }
}

public class RemoveShoppingCartItemCommandHandler
{
    private readonly IShoppingCartEventStore _shoppingCartEventStore;
    
    public RemoveShoppingCartItemCommandHandler(IShoppingCartEventStore shoppingCartEventStore)
    {
        _shoppingCartEventStore = shoppingCartEventStore;
    }
    
    public async Task HandleAsync(RemoveShoppingCartItemCommand command, CancellationToken cancellationToken)
    {
        var shoppingCart = new ShoppingCart();
        
        await _shoppingCart.LoadAsync(
            command.ShoppingCartId.ToString(),
            _shoppingCartEventStore,
            cancellationToken
            );
        
        shoppingCart.RemoveItem(command.ShoppingCartItemId);
        
        await shoppingCart.SaveAsync(_shoppingCartEventStore, cancellationToken);
    }
}
```

### Place the Order
```csharp
public class PlaceShoppingCartOrderCommand
{
    public PlaceShoppingCartOrderCommand(
        Guid shoppingCartId,
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
    
    public Guid ShoppingCartId { get; }
    public string CardNumber { get; }
    public string CardHolder { get; }
    public int CardExpirationYear { get; }
    public int CardExpirationMonth { get; }
    public string CardSecurityCode { get; }
}

public class PlaceShoppingCartOrderCommandHandler
{
    private readonly IShoppingCartEventStore _shoppingCartEventStore;
    private readonly IShoppingCartEventPublisher _shoppingCartEventPublisher;
    private readonly IPaymentService _paymentService;
    
    public PlaceShoppingCartOrderCommandHandler(
        IShoppingCartEventStore shoppingCartEventStore,
        IEventPublisher<ShoppingCartEvent> eventPublisher,
        IPaymentService paymentService
        )
    {
        _shoppingCartEventStore = shoppingCartEventStore;
        _shoppingCartEventPublisher = shoppingCartEventPublisher;
        _paymentService = paymentService;
    }
    
    public async Task HandleAsync(PlaceShoppingCartOrderCommand command, CancellationToken cancellationToken)
    {
        // Create a new ShoppingCart instance
        var shoppingCart = new ShoppingCart(_shoppingCartEventPublisher);
        
        // Load the shopping cart's current state
        await shoppingCart.LoadAsync(
            command.ShoppingCartId.ToString(),
            _shoppingCartEventStore,
            cancellationToken
            );
        
        // Load user data
        var user = await unitOfWork.UserRepository.GetByIdAsync(shoppingCart.UserId, cancellationToken);
        if (user is null)
            throw new UserNotFoundException(shoppingCart.UserId);
        
        // Change the shopping cart state to 'OrderPlaced'
        shoppingCart.PlaceOrder(user.IsPremium);
        
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
        await shoppingCart.SaveAsync(unitOfWork.ShoppingCartEventStore, cancellationToken);
        
        // After persisting changes, the shoppingCart aggregate will use the _shoppingCartEventPublisher instance
        // provided to notify another components of your application about the events applied to the shopping cart.
        // For example, another component of your application could react to a notification after the
        // order has been placed to reserve stock for the involved products and start the shipment process.
    }
}
```

The previous examples were written only to show how to use the abstractions included in this package,
in real applications, there are more elements to consider, such as distributed transactions,
error handling, logging and so on.
