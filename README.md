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
    public ShoppingCartOrderPlaced(
        bool userIsPremium,
        string cardNumber,
        string cardHolder,
        int cardExpirationYear,
        int cardExpirationMonth,
        string cardSecurityCode
        )
    {
        UserIsPremium = userIsPremium;
        CardNumber = cardNumber;
        CardHolder = cardHolder;
        CardExpirationYear = cardExpirationYear;
        CardExpirationMonth = cardExpirationMonth;
        CardSecurityCode = cardSecurityCode;
    }
    
    public bool UserIsPremium { get; }
    public string CardNumber { get; }
    public string CardHolder { get; }
    public int CardExpirationYear { get; }
    public int CardExpirationMonth { get; }
    public string CardSecurityCode { get; }
}
```

## Aggregate Root

In Domain-Driven Design an aggregate root represents the entry point to a cluster of domain
objects (aggregate) treated as a single unit.
An aggregate root ensures the consistency of changes being made within the aggregate boundary
and enforces invariants.

The **AggregateRoot** abstract class provides basic functionality to validate and apply events
to the aggregate roots required by your application.

```csharp
public sealed class ShoppingCart : AggregateRoot<ShoppingCartEvent>
{
    // The AggregateRoot base class defines a Key property as follows:
    
    // public string Key { get; protected set; }
    
    // A convinient value for this property would be the purchase order ID, so all
    // the events related to a single order can be grouped using this key.
  
    // Fictitious repositories and payment service
    private readonly IUserRepository _userRepository;
    private readonly IProductRepository _productRepository;
    private readonly IPaymentServie _paymentService;
  
    public Guid UserId { get; private set; }
    public bool UserIsPremium { get; private set; }
    
    // List of fictitious ShoppingCartItem objects belonging to this shopping cart
    private readonly List<ShoppingCartItem> _items = new ();
    public IEnumerable<ShoppingCartItem> Items => _items;
    
    public ShoppingCartState State { get; private set; }
    public decimal Total => _items.Sum(item => item.TotalPrice);
    public decimal Discount { get; private set; }
    public decimal GrandTotal => Total - Discount;
  
    // The constructor that receives the dependencies of a ShoppingCart object
    public ShoppingCart(
        IUserRepository userRepository,
        IProductRepository productRepository,
        IPaymentService paymentService
        )
    {
        _userRepository = userRepository;
        _productRepository = productRepository;
        _paymentService = paymentService;
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
                
                await _paymentService.ProcessPaymentAsync(
                    orderPlaced.CardNumber,
                    orderPlaced.CardHolder,
                    orderPlaced.CardExpirationYear,
                    orderPlaced.CardExpirationMonth,
                    orderPlaced.CardSecurityCode,
                    GrandTotal,
                    cancellationToken
                    );
                
                State = ShoppingCartState.OrderPlaced;                
                break;
        
            default:
                throw new NotSupportedException($"Event not supported: {@event.GetType().Name}");
        }
    }
  
    public async Task CreateShoppingCartAsync(
        Guid shoppingCartId,
        Guid userId,
        CancellationToken cancellationToken
        )
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            throw new UserNotFoundException(userId);
    
        ApplyChangeAsync(new ShoppingCartCreated(shoppingCartId, user.UserId), cancellationToken);
    }
  
    public async Task AddItemAsync(
        Guid shoppingCartItemId,
        Guid productId,
        double quantity,
        CancellationToken cancellationToken
        )
    {
        var user = await _userRepository.GetByIdAsync(UserId, cancellationToken);
        if (user is null)
            throw new UserNotFoundException(userId);
    
        var product = await _productRepository.GetByIdAsync(productId, cancellationToken);
        if (product is null)
            throw new ProductNotFoundException(productId);
    
        ApplyChangeAsync(new ShoppingCartItemAdded(
            shoppingCartItemId,
            product.ProductId,
            product.Name,
            product.Stock,
            product.Price,
            quantity
            ),
            cancellationToken
            );
    }
  
    public async Task RemoveItemAsync(
        Guid shoppingCartItemId,
        CancellationToken cancellationToken
        )
    {
        ApplyChangeAsync(new ShoppingCartItemRemoved(shoppingCartItemId), cancellationToken);
    }
  
    public async Task PlaceOrderAsync(
        string cardNumber,
        string cardHolder,
        int cardExpirationYear,
        int cardExpirationMonth,
        string cardSecurityCode,
        CancellationToken cancellationToken
        )
    {
        var user = await _userRepository.GetByIdAsync(UserId, cancellationToken);
        if (user is null)
            throw new UserNotFoundException(userId);
    
        ApplyChangeAsync(new ShoppingCartOrderPlaced(
            user.IsPremium,
            cardNumber,
            cardHolder,
            cardExpirationYear,
            cardExpirationMonth,
            cardSecurityCode
            ),
            cancellationToken
            );
    }
  
    // Add more methods to model business behavior
}
```


## Aggregate Root & Command Handlers

It's a common practice to implement the CQRS pattern to perform actions on our aggregate roots.
The following examples demonstrate how to use the ShoppingCart aggregate root to
create and manage the user's shopping cart.

### The Event Store
Define your own interface derived from IEventStore to define an event store for all
the events related to a shopping cart.

```csharp
public interface IShoppingCartEventStore : IEventStore<ShoppingCartEvent>
{
}
```

**Note:** The implementation of this interface is beyond the scope of this library, but you can take a look
at the [Flowsy.EventSourcing.Sql](https://www.nuget.org/packages/Flowsy.EventSourcing.Sql) package, where
you can find base classes to implement event stores powered by a SQL database.


### Create a Purchase Order
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
    private readonly ShoppingCart _shoppingCart;
    private readonly IShoppingCartEventStore _shoppingCartEventStore;
    
    public CreateShoppingCartCommandHandler(ShoppingCart shoppingCart, IShoppingCartEventStore shoppingCartEventStore)
    {
        _shoppingCart = shoppingCart;
        _shoppingCartEventStore = shoppingCartEventStore;
    }
    
    public async Task HandleAsync(CreateShoppingCartCommand command, CancellationToken cancellationToken)
    {
        await _shoppingCart.CreateShoppingCartAsync(
            command.ShoppingCartId,
            command.UserId,
            cancellationToken
            );
        _shoppingCart.SaveAsync(_shoppingCartEventStore, cancellationToken);
    }
}
``` 

### Add Items to a Purchase Order
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
    private readonly ShoppingCart _shoppingCart;
    private readonly IShoppingCartEventStore _shoppingCartEventStore;
    
    public AddShoppingCartItemCommandHandler(ShoppingCart shoppingCart, IShoppingCartEventStore shoppingCartEventStore)
    {
        _shoppingCart = shoppingCart;
        _shoppingCartEventStore = shoppingCartEventStore;
    }
    
    public async Task HandleAsync(AddShoppingCartItemCommand command, CancellationToken cancellationToken)
    {
        await _shoppingCart.LoadAsync(
            command.ShoppingCartId.ToString(),
            _shoppingCartEventStore,
            cancellationToken
            );
        
        await _shoppingCart.AddItemAsync(
            command.ShoppingCartItemId,
            command.ProductId,
            command.Quantity,
            cancellationToken
            );
        
        _shoppingCart.SaveAsync(_shoppingCartEventStore, cancellationToken);
    }
}
```

### Remove Items from a Purchase Order
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
    private readonly ShoppingCart _shoppingCart;
    private readonly IShoppingCartEventStore _shoppingCartEventStore;
    
    public RemoveShoppingCartItemCommandHandler(ShoppingCart shoppingCart, IShoppingCartEventStore shoppingCartEventStore)
    {
        _shoppingCart = shoppingCart;
        _shoppingCartEventStore = shoppingCartEventStore;
    }
    
    public async Task HandleAsync(RemoveShoppingCartItemCommand command, CancellationToken cancellationToken)
    {
        await _shoppingCart.LoadAsync(
            command.ShoppingCartId.ToString(),
            _shoppingCartEventStore,
            cancellationToken
            );
        
        await _shoppingCart.RemoveItemAsync(
            command.ShoppingCartItemId,
            cancellationToken
            );
        
        _shoppingCart.SaveAsync(_shoppingCartEventStore, cancellationToken);
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
    private readonly ShoppingCart _shoppingCart;
    private readonly IShoppingCartEventStore _shoppingCartEventStore;
    
    public PlaceShoppingCartOrderCommandHandler(ShoppingCart shoppingCart, IShoppingCartEventStore shoppingCartEventStore)
    {
        _shoppingCart = shoppingCart;
        _shoppingCartEventStore = shoppingCartEventStore;
    }
    
    public async Task HandleAsync(PlaceShoppingCartOrderCommand command, CancellationToken cancellationToken)
    {
        await _shoppingCart.LoadAsync(
            command.ShoppingCartId.ToString(),
            _shoppingCartEventStore,
            cancellationToken
            );
        
        await _shoppingCart.PlaceOrderAsync(
            command.CardNumber,
            command.CardHolder,
            command.CardExpirationYear,
            command.CardExpirationMonth,
            command.CardSecurityCode,
            cancellationToken
            );
        
        _shoppingCart.SaveAsync(_shoppingCartEventStore, cancellationToken);
    }
}
```

The previous examples were written only to show how to use the abstractions included in this package,
in real applications, there are more elements to consider, such as distributed transactions,
error handling, logging and so on.
