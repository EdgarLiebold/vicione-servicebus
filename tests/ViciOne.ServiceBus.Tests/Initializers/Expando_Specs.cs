namespace ViciOne.ServiceBus.Tests.Initializers
{
    using System;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.Globalization;
    using System.Threading.Tasks;
    using ViciOne.ServiceBus.Initializers;
    using ViciOne.ServiceBus.Initializers.PropertyProviders;
    using System.Text.Json.Nodes;
    using NUnit.Framework;


    [TestFixture]
    public class Initializing_using_a_dictionary
    {
        [Test]
        public async Task Should_do_the_right_thing()
        {
            var uniqueId = Guid.NewGuid();
            IDictionary<string, object> dto = new ExpandoObject();
            dto.Add(nameof(MessageContract.Id), 27);
            dto.Add(nameof(MessageContract.CustomerId), "SuperMart");
            dto.Add(nameof(MessageContract.UniqueId), uniqueId);
            dto.Add(nameof(MessageContract.CustomerType), (long)1);
            dto.Add(nameof(MessageContract.TypeByName), "Internal");

            InitializeContext<MessageContract> message = await MessageInitializerCache<MessageContract>.Initialize(dto);

            Assert.Multiple(() =>
            {
                Assert.That(message.Message.Id, Is.EqualTo(27));
                Assert.That(message.Message.CustomerId, Is.EqualTo("SuperMart"));
                Assert.That(message.Message.UniqueId, Is.EqualTo(uniqueId));
                Assert.That(message.Message.CustomerType, Is.EqualTo(CustomerType.Public));
                Assert.That(message.Message.TypeByName, Is.EqualTo(CustomerType.Internal));
            });
        }

        [Test]
        public void Should_have_an_interface_from_dictionary_converter()
        {
            var factory = new PropertyProviderFactory<IDictionary<string, object>>();
            Assert.That(factory.TryGetPropertyConverter(out IPropertyConverter<MessageContract, object> converter), Is.True);
        }

        [Test]
        public async Task Should_properly_handle_the_big_dto()
        {
            var order = new OrderDto
            {
                Amount = 123.45m,
                Id = 27,
                CustomerId = "FRANK01",
                ItemType = "Crayon",
                OrderState = new OrderState(OrderStatus.Validated),
                TokenizedCreditCard = new TokenizedCreditCardDto
                {
                    ExpirationMonth = "12",
                    ExpirationYear = "2019",
                    PublicKey = new JsonObject { ["key"] = "12345" },
                    Token = new JsonObject { ["value"] = "Token123" }
                }
            };

            var correlationId = Guid.NewGuid();
            InitializeContext<IPaymentGatewaySubmittedEvent> message = await MessageInitializerCache<IPaymentGatewaySubmittedEvent>.Initialize(new
            {
                order,
                correlationId,
                TimeStamp = DateTime.Now,
                ConsumerProcessed = true
            });

            Assert.Multiple(() =>
            {
                Assert.That(message.Message.CorrelationId, Is.EqualTo(correlationId));
                Assert.That(message.Message.Order, Is.Not.Null);
            });
            Assert.That(message.Message.Order.OrderState, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(message.Message.Order.OrderState.Status, Is.EqualTo(OrderStatus.Validated));
                Assert.That(message.Message.Order.TokenizedCreditCard, Is.Not.Null);
            });
            Assert.That(message.Message.Order.TokenizedCreditCard.ExpirationMonth, Is.EqualTo("12"));
        }

        [Test]
        public async Task Should_work_with_a_dictionary()
        {
            var uniqueId = Guid.NewGuid();
            IDictionary<string, object> dto = new Dictionary<string, object>();
            dto.Add(nameof(MessageContract.Id), 27);
            dto.Add(nameof(MessageContract.CustomerId), "SuperMart");
            dto.Add(nameof(MessageContract.UniqueId), uniqueId);

            InitializeContext<MessageContract> message = await MessageInitializerCache<MessageContract>.Initialize(dto);

            Assert.Multiple(() =>
            {
                Assert.That(message.Message.Id, Is.EqualTo(27));
                Assert.That(message.Message.CustomerId, Is.EqualTo("SuperMart"));
                Assert.That(message.Message.UniqueId, Is.EqualTo(uniqueId));
            });
        }

        [Test]
        public async Task Should_work_with_a_dictionary_sourced_object_property()
        {
            var uniqueId = Guid.NewGuid();
            IDictionary<string, object> dto = new Dictionary<string, object>();
            dto.Add(nameof(MessageContract.Id), 27);
            dto.Add(nameof(MessageContract.CustomerId), "SuperMart");
            dto.Add(nameof(MessageContract.UniqueId), uniqueId);

            InitializeContext<MessageEnvelope> message = await MessageInitializerCache<MessageEnvelope>.Initialize(new { Contract = dto });

            Assert.That(message.Message.Contract, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(message.Message.Contract.Id, Is.EqualTo(27));
                Assert.That(message.Message.Contract.CustomerId, Is.EqualTo("SuperMart"));
                Assert.That(message.Message.Contract.UniqueId, Is.EqualTo(uniqueId));
            });
        }

        [Test]
        public async Task Should_work_with_a_list()
        {
            var dto = new
            {
                Id = 32,
                CustomerId = "CustomerXp",
                Product = new
                {
                    Name = "Foo",
                    Category = "Bar"
                },
                Orders = new[]
                {
                    new
                    {
                        Id = Guid.NewGuid(),
                        Product = new
                        {
                            Name = "Product",
                            Category = "Category"
                        },
                        Quantity = 10,
                        Price = 10.0m
                    }
                }
            };

            // The expando is built here rather than materialised by a serializer. What this case is
            // about is the initializer reading a nested list out of an expando, and the runtime types
            // that reaches it with — a nested ExpandoObject, a List<object>, integers as long — used to
            // be a by-product of Json.NET's expando materialisation. Stating them makes the shape the
            // assertion depends on visible instead of hiding it behind a second serializer.
            dynamic expandoProduct = new ExpandoObject();
            expandoProduct.Name = dto.Product.Name;
            expandoProduct.Category = dto.Product.Category;

            dynamic expandoOrderProduct = new ExpandoObject();
            expandoOrderProduct.Name = dto.Orders[0].Product.Name;
            expandoOrderProduct.Category = dto.Orders[0].Product.Category;

            dynamic expandoOrder = new ExpandoObject();
            expandoOrder.Id = dto.Orders[0].Id.ToString();
            expandoOrder.Product = expandoOrderProduct;
            expandoOrder.Quantity = (long)dto.Orders[0].Quantity;
            expandoOrder.Price = (double)dto.Orders[0].Price;

            dynamic expando = new ExpandoObject();
            expando.Id = (long)dto.Id;
            expando.CustomerId = dto.CustomerId;
            expando.Product = expandoProduct;
            expando.Orders = new List<object> { expandoOrder };

            InitializeContext<MessageContract> message =
                await MessageInitializerCache<MessageContract>.Initialize((ExpandoObject)expando);

            Assert.Multiple(() =>
            {
                Assert.That(message.Message.Id, Is.EqualTo(32));

                // Is.Not.Null alone let this case pass with an empty list, which is exactly the
                // outcome the historical comment claimed it was catching. The nested order has to
                // arrive, so the count and the values below it are asserted rather than its presence.
                Assert.That(message.Message.Orders, Has.Count.EqualTo(1));
                Assert.That(message.Message.Orders[0].Quantity, Is.EqualTo(10));
                Assert.That(message.Message.Orders[0].Product, Is.Not.Null);
                Assert.That(message.Message.Orders[0].Product.Name, Is.EqualTo("Product"));
            });
        }


        public interface IPaymentGatewaySubmittedEvent : IPaymentBase
        {
        }


        public interface IPaymentBase :
            CorrelatedBy<Guid>
        {
            DateTime TimeStamp { get; }
            string Status { get; set; }
            OrderDto Order { get; }
            bool ConsumerProcessed { get; }
        }


        public class OrderDto
        {
            public OrderDto()
            {
                if (OrderState == null)
                    OrderState = new OrderState(OrderStatus.ClientSubmitted);

                if (BillTo == null)
                    BillTo = new AddressDto();

                if (TokenizedCreditCard == null)
                    TokenizedCreditCard = new TokenizedCreditCardDto();
            }

            public int Id { get; set; }
            public decimal Amount { get; set; }

            public string ItemType { get; set; }
            public OrderState OrderState { get; set; }

            public AddressDto BillTo { get; set; }
            public TokenizedCreditCardDto TokenizedCreditCard { get; set; }

            public string MerchantType { get; set; }

            public string CustomerId { get; set; }

            public void SetAmount(decimal amount)
            {
                Amount = amount;
            }

            public string FormattedAmount()
            {
                return Amount == default ? "$0.00" : Amount.ToString("#.00", CultureInfo.InvariantCulture);
            }
        }


        public class AddressDto
        {
            public string PostalCode { get; set; }
        }


        public class OrderState
        {
            public OrderState()
            {
            }

            public OrderState(OrderStatus status)
            {
                Status = status;
            }

            public bool IsValidOrder => Status == OrderStatus.Validated;

            public OrderStatus Status { get; set; }
            public string Message { get; set; }

            public DateTimeOffset SubmitTimeUtc { get; set; }
        }


        public enum OrderStatus
        {
            ClientSubmitted,
            Validated,
            Duplicate,
            AUTHORIZED,
            PARTIALAUTHORIZED,
            AUTHORIZEDPENDINGREVIEW,
            DECLINED,
            INVALIDREQUEST,
            PENDING,
            ProcessingError,
            Saved
        }


        public class TokenizedCreditCardDto
        {
            public string ExpirationMonth { get; set; }
            public string ExpirationYear { get; set; }
            public JsonObject PublicKey { get; set; }
            public JsonObject Token { get; set; }
            public string SecurityCode { get; set; }
        }


        public interface MessageContract
        {
            int Id { get; }
            string CustomerId { get; }
            Guid UniqueId { get; }
            CustomerType CustomerType { get; }
            CustomerType TypeByName { get; }
            List<IOrder> Orders { get; }
        }


        public enum CustomerType
        {
            Public = 1,
            Internal = 2
        }


        public interface IOrder
        {
            Guid Id { get; }
            IProduct Product { get; }
            int Quantity { get; }
        }


        public interface IProduct
        {
            string Name { get; }
            string Category { get; }
        }


        public interface MessageEnvelope
        {
            MessageContract Contract { get; }
        }
    }
}
