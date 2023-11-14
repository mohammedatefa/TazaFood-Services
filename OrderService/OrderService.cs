using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TazaFood_Core.IRepositories;
using TazaFood_Core.Models;
using TazaFood_Core.Models.Order_Aggregate;
using TazaFood_Core.Services;

namespace TazaFood_Services.Order
{
    public class OrderService : IOrderService
    {
        private readonly ICartItemsRepository cartItemRepo;
        private readonly IUnitOfWork unitOfWork;

        public OrderService(ICartItemsRepository CartItemRepo,IUnitOfWork UnitOfWork)
        {
            cartItemRepo = CartItemRepo;
            unitOfWork = UnitOfWork;
        }
        public async Task<TazaFood_Core.Models.Order_Aggregate.Order?> CreateOrder(string userEmail, string cartId, int deliveryMethodId, Address shippingAddress)
        {
            //get cart from cart repo 
            var cartItem = await cartItemRepo.GetCartAsync(cartId);

            //get selected Items from product repo 
            var orderitems = new List<OrderItem>();

            if (cartItem?.CartItems?.Count > 0)
            {
                foreach (var item in cartItem.CartItems)
                {
                    var product = await unitOfWork.Repository<Product>().GetById(item.Id);
                    var productItemOrdere = new ProductOrderItem(product.Id, product.Name, product.ImageUrl);
                    var orderItem = new OrderItem(productItemOrdere, item.Quantity, product.Price);
                    orderitems.Add(orderItem);
                }
            }

            //calculate the subtotal
            var subTotal = orderitems.Sum(o => (o.Price * o.Quantity));

            //get delivery method from deliverymethod repo 
            var deliveryMethod = await unitOfWork.Repository<DeliveryMethod>().GetById(deliveryMethodId);

            //create order 
            var order = new TazaFood_Core.Models.Order_Aggregate.Order(userEmail, shippingAddress, deliveryMethod, orderitems, subTotal);
            //add order to order data
             await unitOfWork.Repository<TazaFood_Core.Models.Order_Aggregate.Order>().Add(order);

            //save to data base 
            var resualt= await unitOfWork.complete();
            if (resualt <= 0) return null;
            return order;

        }

        public Task<IReadOnlyList<TazaFood_Core.Models.Order_Aggregate.Order>> GetAllOrdersForUser(string userEmail)
        {
            throw new NotImplementedException();
            //var orders = unitOfWork.Repository<TazaFood_Core.Models.Order_Aggregate.Order>().GetAllWithSpec();
        }

        public Task<TazaFood_Core.Models.Order_Aggregate.Order> GetOrderById(int orderId, string userEmail)
        {
            throw new NotImplementedException();
        }
    }
}
