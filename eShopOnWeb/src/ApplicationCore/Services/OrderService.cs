using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using Microsoft.eShopWeb.ApplicationCore.Entities;
using Microsoft.eShopWeb.ApplicationCore.Entities.BasketAggregate;
using Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate;
using Microsoft.eShopWeb.ApplicationCore.Interfaces;
using Microsoft.eShopWeb.ApplicationCore.Specifications;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace Microsoft.eShopWeb.ApplicationCore.Services;

public class OrderService : IOrderService
{
    private readonly IRepository<Order> _orderRepository;
    private readonly IUriComposer _uriComposer;
    private readonly IRepository<Basket> _basketRepository;
    private readonly IRepository<CatalogItem> _itemRepository;
    private readonly IConfiguration _configuration;

    public OrderService(IRepository<Basket> basketRepository,
        IRepository<CatalogItem> itemRepository,
        IRepository<Order> orderRepository,
        IUriComposer uriComposer,
        IConfiguration configuration)
    {
        _orderRepository = orderRepository;
        _uriComposer = uriComposer;
        _basketRepository = basketRepository;
        _itemRepository = itemRepository;
        _configuration = configuration;
    }

    public async Task CreateOrderAsync(int basketId, Address shippingAddress)
    {
        var basketSpec = new BasketWithItemsSpecification(basketId);
        var basket = await _basketRepository.GetBySpecAsync(basketSpec);

        Guard.Against.NullBasket(basketId, basket);
        Guard.Against.EmptyBasketOnCheckout(basket.Items);

        var catalogItemsSpecification = new CatalogItemsSpecification(basket.Items.Select(item => item.CatalogItemId).ToArray());
        var catalogItems = await _itemRepository.ListAsync(catalogItemsSpecification);

        var items = basket.Items.Select(basketItem =>
        {
            var catalogItem = catalogItems.First(c => c.Id == basketItem.CatalogItemId);
            var itemOrdered = new CatalogItemOrdered(catalogItem.Id, catalogItem.Name, _uriComposer.ComposePicUri(catalogItem.PictureUri));
            var orderItem = new OrderItem(itemOrdered, basketItem.UnitPrice, basketItem.Quantity);
            return orderItem;
        }).ToList();

        var order = new Order(basket.BuyerId, shippingAddress, items);

        await _orderRepository.AddAsync(order);

        await SendOrderToDeliveryService(order);
    }

    private async Task SendOrderToWarehouse(List<OrderItem> items)
    {
        HttpClient client = new HttpClient();
        HttpContent content = new StringContent(JsonConvert.SerializeObject(items, Formatting.Indented));
        await client.PostAsync(_configuration.GetSection("OrderItemsReserverUrl").Value, content);
    }

    private async Task SendOrderToDeliveryService(Order order)
    {
        HttpClient client = new HttpClient();
        var deliveryObject = new OrderDelivery
        {
            Id = $"eShopOrder{new System.Random().Next()}",
            ShipAddress = $"{order.ShipToAddress.State}, {order.ShipToAddress.City}, {order.ShipToAddress.Street}, {order.ShipToAddress.ZipCode}",
            Items = order.OrderItems.Select(o => o.ItemOrdered.ProductName).ToList(),
            FinalPrice = (int)order.OrderItems.Sum(o => o.UnitPrice * o.Units)
        };
        HttpContent content = new StringContent(JsonConvert.SerializeObject(deliveryObject, Formatting.Indented));
        await client.PostAsync(_configuration.GetSection("OrderDeliveryServiceUrl").Value, content);
    }
}

public class OrderDelivery
{
    public string Id { get; set; }
    public string ShipAddress { get; set; }
    public List<string> Items { get; set; }
    public int FinalPrice { get; set; }
}

