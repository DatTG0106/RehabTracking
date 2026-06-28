using RehabTracking.Web.Entities;

namespace RehabTracking.Web.Features.ECommerce.Cart;

public class CartItem
{
    public Product Product { get; set; } = null!;
    public int Quantity { get; set; }
}

public class CartState
{
    public List<CartItem> Items { get; private set; } = new();

    public event Action? OnChange;

    public void AddToCart(Product product, int quantity = 1)
    {
        var existingItem = Items.FirstOrDefault(i => i.Product.ProductId == product.ProductId);
        if (existingItem != null)
        {
            existingItem.Quantity += quantity;
        }
        else
        {
            Items.Add(new CartItem { Product = product, Quantity = quantity });
        }
        NotifyStateChanged();
    }

    public void RemoveFromCart(int productId)
    {
        var item = Items.FirstOrDefault(i => i.Product.ProductId == productId);
        if (item != null)
        {
            Items.Remove(item);
            NotifyStateChanged();
        }
    }

    public void DecreaseQuantity(int productId)
    {
        var item = Items.FirstOrDefault(i => i.Product.ProductId == productId);
        if (item != null && item.Quantity > 1)
        {
            item.Quantity--;
            NotifyStateChanged();
        }
        else if (item != null && item.Quantity <= 1)
        {
            Items.Remove(item);
            NotifyStateChanged();
        }
    }

    public void ClearCart()
    {
        Items.Clear();
        NotifyStateChanged();
    }

    public decimal GetTotal()
    {
        return Items.Sum(i => i.Product.Price * i.Quantity);
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
