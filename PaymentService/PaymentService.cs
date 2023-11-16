using Microsoft.Extensions.Configuration;
using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TazaFood_Core.IRepositories;
using TazaFood_Core.ISpecifications;
using TazaFood_Core.Models;
using TazaFood_Core.Models.Order_Aggregate;
using TazaFood_Core.Services;
using Product = TazaFood_Core.Models.Product;

namespace TazaFood_Services.PaymentService
{
    public class PaymentService : IPaymentService
    {
        private readonly IConfiguration configuration;
        private readonly ICartItemsRepository cartItemsRepository;
        private readonly IUnitOfWork unitOfWork;

        public PaymentService(IConfiguration Configuration,
            ICartItemsRepository CartItemsRepository,
            IUnitOfWork UnitOfWork
            )
        {
            configuration = Configuration;
            cartItemsRepository = CartItemsRepository;
            unitOfWork = UnitOfWork;
        }
        public async Task<UserCart> CreateOrUpdatePaymentIntent(string cartId)
        {
            StripeConfiguration.ApiKey = configuration["Stripe:Secretkey"];

            var cart = await cartItemsRepository.GetCartAsync(cartId);

            if (cart is null) return null;
            var deleverymethodId = cart.DeleveryMethodId;
            var shippingPrice = 0m;
            if (deleverymethodId.HasValue)
            {
                var DeleveryMethod = await unitOfWork.Repository<DeliveryMethod>().GetById(deleverymethodId.Value);
                cart.ShippingCost = DeleveryMethod.Cost;
                shippingPrice = DeleveryMethod.Cost;
            }

            if (cart?.CartItems.Count > 0)
            {
                foreach (var item in cart.CartItems)
                {
                    var product = await unitOfWork.Repository<Product>().GetById(item.Id);
                    if (product.Price != item.Price)
                    {
                        item.Price = product.Price;
                    }                    
                }
            }

            PaymentIntent paymentIntent;

            var servic = new PaymentIntentService();
            if (string.IsNullOrEmpty(cart.PaymentIntentId))
            {
                //create payment intent
                var options = new PaymentIntentCreateOptions()
                {
                    Amount = (long)cart.CartItems.Sum(item => item.Price * item.Quantity * 100)+(long)(shippingPrice*100) ,
                    Currency="usd",
                    PaymentMethodTypes=new List<string>(){"card"}
                };
                paymentIntent= await servic.CreateAsync(options);
                cart.PaymentIntentId = paymentIntent.Id;
                cart.ClientSecrete = paymentIntent.ClientSecret;
            }
            else
            {
                //create payment intent
                var options = new PaymentIntentUpdateOptions()
                {
                    Amount = (long)cart.CartItems.Sum(item => item.Price * item.Quantity * 100) + (long)(shippingPrice * 100),
                };

                await servic.UpdateAsync(cart.PaymentIntentId, options);
            }

            await cartItemsRepository.UpdateCartAsync(cart);
            return cart;

        }

        public async Task<TazaFood_Core.Models.Order_Aggregate.Order> UpdatePaymentIntentWithSucceededOrFaild(string paymentIntent, bool IsSucceeded)
        {
            //first find the order 
            var spec = new OrderWithPaymentIntenSpecification(paymentIntent);
            var order = await unitOfWork.Repository<TazaFood_Core.Models.Order_Aggregate.Order>().GetByIdWithSpec(spec);

            if (IsSucceeded)
                order.OrderStaus = DeliveryStatus.PaymentRecived;
            else
                order.OrderStaus = DeliveryStatus.PaymentFailed;
            await unitOfWork.Repository<TazaFood_Core.Models.Order_Aggregate.Order>().Update(order.Id, order);
            await unitOfWork.complete();
            return order;
        }
    }
}
