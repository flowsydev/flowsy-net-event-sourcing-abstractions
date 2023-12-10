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
    DateTimeOffset InitiationInstant { get; }
}
```

For example, we can define a ShoppingCartEvent abstract class and use it as the
base class for all the events related to a given shopping cart.

```csharp
public abstract class ShoppingCartEvent : IEvent
{
    protected ShoppingCartEvent()
    {
        InitiationInstant = DateTimeOffset.Now;
    }
    
    DateTimeOffset InitiationInstant { get; }
}

public sealed class ShoppingCartCreated : ShoppingCartEvent
{
    public ShoppingCartCreated(string shoppingCartId, string userId)
    {
        ShoppingCartId = shoppingCartId;
        UserId = userId;
    }

    public string ShoppingCartId { get; }
    public string UserId { get; }
}

public sealed class ShoppingCartItemAdded : ShoppingCartEvent
{
    public ShoppingCartItemAdded(
        string shoppingCartItemId,
        string productId,
        decimal productPrice,
        double quantity
        )
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
    public ShoppingCartItemRemoved(string shoppingCartItemId)
    {
        ShoppingCartItemId = shoppingCartItemId;
    }
  
    public string ShoppingCartItemId { get; }
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
* Company.Project.Sales.Domain.Entities.Views.ShoppingCart: Read model used to browse a list of shopping carts.
* Company.Project.Sales.Domain.Entities.Views.ShoppingCartItem: Read model used to browse the items of a give shopping cart.
* Company.Project.Sales.Domain.Entities.Views.Product: Read model used to browse a list of products.

As you can see, you can have more than one class with the same name, but organized in different namespaces.
If you want to, you can also use different names or add suffixes to avoid confusion, for example:
* Company.Project.Sales.Domain.Entities.Product
* Company.Project.Sales.Domain.Entities.Vies.ProductView (ProductReadModel, ProductDto or whatever convention you prefer)

The important thing is to define your own conventions and to be consistent throughout your application.

The **AggregateRoot** abstract class in this package provides basic functionality to validate and apply events
to a given aggregate root required by your application.

```csharp
public sealed class ShoppingCart : AggregateRoot
{
    public string UserId { get; private set; }
    public bool UserIsPremium { get; private set; }
    
    // List of fictitious ShoppingCartItem objects belonging to this shopping cart
    private readonly List<ShoppingCartItem> _items = new ();
    public IEnumerable<ShoppingCartItem> Items => _items;
    
    public ShoppingCartStatus Status { get; private set; }
    public decimal Total => _items.Sum(item => item.TotalPrice);
    public decimal Discount { get; private set; }
    public decimal GrandTotal => Total - Discount;
  
    // Override the Apply method to provide the required actions for each type of event
    protected override void Apply(IEvent @event)
    {
        switch (@event)
        {
            case ShoppingCartCreated e:
                // The AggregateRoot base class defines a Key property of type string.                
                // A convinient value for this property would be the shopping cart ID, so all
                // the events related to a single shopping cart can be grouped using this key.
                Key = e.ShoppingCartId.ToString();
                UserId = e.UserId;
                UserIsPremium = false;
                Discount = 0m;
                break;
        
            case ShoppingCartItemAdded e:
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
                break;
        
            case ShoppingCartItemRemoved e:
                var item = _items.Find(i => i.ShoppingCartItemId == e.ShoppingCartItemId);
                _items.Remove(item);
                Status = _items.Any() ? ShoppingCartStatus.Active : ShoppingCartStatus.Empty;
                break;
        
            // Apply other events
            // ...
            
            case ShoppingCartOrderPlaced orderPlaced:
                UserIsPremium = orderPlaced.UserIsPremium;                
                
                if (UserIsPremium)
                    Discount = Total * 0.15m;
                
                Status = ShoppingCartStatus.OrderPlaced;                
                break;
        
            default:
                throw new NotSupportedException($"Event not supported: {@event.GetType().Name}");
        }
    }
  
    public void CreateNew(string shoppingCartId, User user)
    {
        ApplyChange(new ShoppingCartCreated(shoppingCartId, User.UserId));
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
The following examples demonstrate how to use the ShoppingCart aggregate root to
create and manage the user's shopping cart.

### The Aggregate Repository
This package includes the **IAggregateRepository** interface with methods to store and load aggregates in some event store.

**Note:** The implementation of this interface is beyond the scope of this library, but you can take a look
at the [Flowsy.EventSourcing.Sql](https://www.nuget.org/packages/Flowsy.EventSourcing.Sql) package, which includes an implementation based on [Marten](https://martendb.io/),
a document database and event store library with PostgreSQL as the underlying database.


### Create a Shopping Cart
```csharp
public class CreateShoppingCartCommand
{
    public CreateShoppingCartCommand(string shoppingCartId, string userId)
    {
        ShoppingCartId = shoppingCartId;
        UserId = userId;
    }
    
    public string ShoppingCartId { get; }
    public string UserId { get; }
}

public class CreateShoppingCartCommandHandler
{
    private readonly IAggregateRepository _aggregateRepository;
    private readonly IUserRepository _userRepository;
    
    public CreateShoppingCartCommandHandler(
        IAggregateRepository aggregateRepository,
        IUserRepository userRepository
        )
    {
        _aggregateRepository = aggregateRepository;
        _userRepository = userRepository;
    }
    
    public async Task HandleAsync(CreateShoppingCartCommand command, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null)
            throw new UserNotFoundException(userId);
            
        var shoppingCart = new ShoppingCart();        
        shoppingCart.CreateNew(command.ShoppingCartId, user);
        
        await _aggregateRepository.StoreAsync(shoppingCart, cancellationToken);
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
    private readonly IAggregateRepository _aggregateRepository;
    private readonly IProductRepository _productRepository;
    
    public AddShoppingCartItemCommandHandler(
        IAggregateRepository aggregateRepository,
        IProductRepository productRepository
        )
    {
        _aggregateRepository = aggregateRepository;
        _productRepository = productRepository;
    }
    
    public async Task HandleAsync(AddShoppingCartItemCommand command, CancellationToken cancellationToken)
    {
        var shoppingCart = _aggregateRepository.LoadAsync(command.ShoppingCartId, cancellationToken);
        if (shoppingCart is null)
            throw new EntityNotFoundException($"Shopping cart not found: {command.ShoppingCartId}");
        
        var product = await _productRepository.GetByIdAsync(command.ProductId, cancellationToken);
        if (product is null)
            throw new EntityNotFoundException($"Product not found: {command.ProductId}");
        
        shoppingCart.AddItem(
            command.ShoppingCartItemId,
            product,
            command.Quantity
            );
        
        await _aggregateRepository.StoreAsync(shoppingCart, cancellationToken);
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
    private readonly IAggregateRepository _aggregateRepository;
    
    public RemoveShoppingCartItemCommandHandler(IAggregateRepository aggregateRepository)
    {
        _aggregateRepository = aggregateRepository;
    }
    
    public async Task HandleAsync(RemoveShoppingCartItemCommand command, CancellationToken cancellationToken)
    {
        var shoppingCart = _aggregateRepository.LoadAsync(command.ShoppingCartId, cancellationToken);
        if (shoppingCart is null)
            throw new EntityNotFoundException($"Shopping cart not found: {command.ShoppingCartId}");
        
        shoppingCart.RemoveItem(command.ShoppingCartItemId);
        
        await _aggregateRepository.StoreAsync(shoppingCart, cancellationToken);
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
    private readonly IAggregateRepository _aggregateRepository;
    private readonly IPaymentService _paymentService;
    
    public PlaceShoppingCartOrderCommandHandler(
        IAggregateRepository aggregateRepository;,
        IPaymentService paymentService
        )
    {
        _aggregateRepository = aggregateRepository;
        _paymentService = paymentService;
    }
    
    public async Task HandleAsync(PlaceShoppingCartOrderCommand command, CancellationToken cancellationToken)
    {
        var shoppingCart = _aggregateRepository.LoadAsync(command.ShoppingCartId, cancellationToken);
        if (shoppingCart is null)
            throw new EntityNotFoundException($"Shopping cart not found: {command.ShoppingCartId}");
        
        // Load user data
        var user = await unitOfWork.UserRepository.GetByIdAsync(shoppingCart.UserId, cancellationToken);
        if (user is null)
            throw new EntityNotFoundException($"User not found: {shoppingCart.UserId}");
        
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
        await _aggregateRepository.StoreAsync(shoppingCart, cancellationToken);
    }
}
```

The previous examples were written only to show how to use the abstractions included in this package,
in real applications, there are more elements to consider, such as distributed transactions,
error handling, logging and so on.
