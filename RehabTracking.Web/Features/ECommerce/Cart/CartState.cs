using RehabTracking.Web.Entities;

namespace RehabTracking.Web.Features.ECommerce.Cart;

public class CartItem
{
    public Product Product { get; set; } = null!;
    public int Quantity { get; set; }
}

/// <summary>
/// CartState quản lý giỏ hàng per-user (per Blazor circuit).
/// 
/// QUAN TRỌNG: Phải đăng ký là AddScoped (KHÔNG phải AddSingleton) trong Program.cs.
/// - Singleton = chia sẻ giỏ hàng giữa TẤT CẢ users → BUG nghiêm trọng
/// - Scoped    = mỗi Blazor circuit (= mỗi tab/session) có giỏ hàng riêng → ĐÚNG
///
/// Giỏ hàng sẽ bị xóa khi user:
///   1. Đóng tab trình duyệt (circuit dispose)
///   2. Đăng xuất (gọi ClearCart() trong NavBar)
///   3. Hoàn tất thanh toán (gọi ClearCart() trong Checkout)
/// </summary>
public class CartState
{
    private readonly Dictionary<int, List<CartItem>> _userCarts = new();
    private int _currentUserId = 0;

    /// <summary>
    /// Gán UserId cho CartState để isolate giỏ hàng theo user.
    /// Gọi phương thức này ngay sau khi authentication state thay đổi.
    /// </summary>
    public void SetUser(int userId)
    {
        if (_currentUserId != userId)
        {
            _currentUserId = userId;
            // Không xóa cart của user khi switch - chỉ switch context
            NotifyStateChanged();
        }
    }

    /// <summary>Giỏ hàng của user hiện tại.</summary>
    public List<CartItem> Items
    {
        get
        {
            if (_currentUserId <= 0)
                return new List<CartItem>(); // Chưa đăng nhập: trả về empty (read-only view)
            if (!_userCarts.TryGetValue(_currentUserId, out var cart))
            {
                cart = new List<CartItem>();
                _userCarts[_currentUserId] = cart;
            }
            return cart;
        }
    }

    public event Action? OnChange;

    public void AddToCart(Product product, int quantity = 1)
    {
        if (_currentUserId <= 0) return; // Không cho add nếu chưa set user
        if (quantity <= 0) return;

        var items = Items;
        var existingItem = items.FirstOrDefault(i => i.Product.ProductId == product.ProductId);
        if (existingItem != null)
        {
            existingItem.Quantity += quantity;
        }
        else
        {
            items.Add(new CartItem { Product = product, Quantity = quantity });
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
        if (_currentUserId > 0 && _userCarts.ContainsKey(_currentUserId))
        {
            _userCarts[_currentUserId].Clear();
        }
        NotifyStateChanged();
    }

    /// <summary>
    /// Xóa toàn bộ giỏ hàng khi user đăng xuất.
    /// Gọi trước khi SetUser(0).
    /// </summary>
    public void ClearCartForUser(int userId)
    {
        if (_userCarts.ContainsKey(userId))
        {
            _userCarts.Remove(userId);
        }
        NotifyStateChanged();
    }

    public decimal GetTotal()
    {
        return Items.Sum(i => i.Product.Price * i.Quantity);
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
