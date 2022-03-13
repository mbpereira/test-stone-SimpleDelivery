﻿using Core.Orders.Contracts;
using Data.Entities.Catalog;
using Data.Entities.Sale;
using Data.Repositories.Catalog.Contracts;
using Data.Repositories.Sale.Contracts;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Orders
{
    public class OrdersHandler : IOrdersHandler
    {
        private readonly IOrdersRepository _orders;
        private readonly ICustomersRepository _customers;
        private readonly IProductsRepository _products;

        public OrdersHandler(IOrdersRepository orders, ICustomersRepository customers, IProductsRepository products)
        {
            _orders = orders;
            _customers = customers;
            _products = products;
        }

        public async Task Create(Order order)
        {
            order.CreatedAt = System.DateTime.Now.Date;
            await ValidateBasicData(order);

            await UpdateStockOfNewOrderItems(order);

            await _orders.Add(order);
        }

        public async Task Cancel(int idOrder)
        {
            var order = await _orders.GetByKey(idOrder);
            await ValidateBasicData(order);
            await Remove(order);
            order.Status = OrderStatus.Canceled;
            await _orders.Update(order);
        }

        public async Task Delete(int idOrder)
        {
            var order = await _orders.GetByKey(idOrder);
            await ValidateBasicData(order);
            await Remove(order);
            await _orders.DeleteByKey(idOrder);
        }
        
        public async Task Update(Order updatedOrder)
        {
            var oldOrder = await _orders.GetByKey(updatedOrder.Id);
            oldOrder.UpdatedAt = System.DateTime.Now;

            await ValidateBasicData(oldOrder);

            oldOrder.CustomerId = updatedOrder.CustomerId;
            oldOrder.ShipmentValue = updatedOrder.ShipmentValue;
            oldOrder.Status = updatedOrder.Status;

            await UpdateStockOfNewOrderItems(updatedOrder);
            await SyncStockOfUpdatedOrderItems(updatedOrder, oldOrder);
            await RollbackStockOfRemovedOrderItems(updatedOrder, oldOrder);

            oldOrder.Itens = updatedOrder.Itens;

            await _orders.Update(oldOrder);
        }

        private async Task UpdateStockOfNewOrderItems(Order order)
        {
            var isApproved = order.IsApproved();

            foreach (var item in order.Itens.Where(i => i.Id.Equals(0)))
            {
                var product = await _products.GetByKey(item.ProductId);

                ValidateItem(item, product, isApproved);

                if (!isApproved) continue;

                ApplyDefaultValuesIfNecessary(item, product);
                product.Stock -= item.Quantity;
                await UpdateProduct(product);
            }
        }

        private async Task Remove(Order order)
        {
            var isApproved = order.IsApproved();
            order.UpdatedAt = System.DateTime.Now.Date;
            await RemoveItems(order.Itens, isApproved);
        }

        private async Task SyncStockOfUpdatedOrderItems(Order updatedOrder, Order oldOrder)
        {
            var isApproved = updatedOrder.IsApproved();
            var wasApproved = oldOrder.IsApproved();

            foreach(var updatedOrderItem in updatedOrder.Itens.Where(i => i.Id > 0))
            {
                var oldOrderItem = oldOrder.Itens.FirstOrDefault(i => i.Id.Equals(updatedOrderItem.Id));
                var oldProduct = await _products.GetByKey(oldOrderItem.ProductId);
                var newProduct = await _products.GetByKey(updatedOrderItem.ProductId);

                if (wasApproved)
                {
                    oldProduct.Stock += oldOrderItem.Quantity;
                    await UpdateProduct(oldProduct);
                }

                ValidateItem(updatedOrderItem, newProduct, isApproved);

                if (!isApproved) continue;

                ApplyDefaultValuesIfNecessary(updatedOrderItem, newProduct);
                newProduct.Stock -= updatedOrderItem.Quantity;
                await UpdateProduct(newProduct);
            }
        }

        private async Task UpdateProduct(Product product)
        {
            if (product.Stock < 0)
                throw new System.Exception($"insufficient stock for {product.Description}");
            await _products.Update(product);
        }

        private async Task RemoveItems(IEnumerable<OrderItem> removedItems, bool wasApproved)
        {
            foreach (var item in removedItems)
            {
                var product = await _products.GetByKey(item.ProductId);

                if (!wasApproved) continue;

                product.Stock += item.Quantity;
                await UpdateProduct(product);
            }
        }

        private async Task RollbackStockOfRemovedOrderItems(Order updatedOrder, Order oldOrder)
        {
            var removedItems = oldOrder.Itens.Where(oldItem => !updatedOrder.Itens.Any(item => item.Id.Equals(oldItem.Id)));
            var wasApproved = oldOrder.IsApproved();
            await RemoveItems(removedItems, wasApproved);
        }

        private void ApplyDefaultValuesIfNecessary(OrderItem item, Product product)
        {
            if (item.UnitCost <= 0)
                item.UnitCost = product.Cost;
            if (item.UnitPrice <= 0)
                item.UnitPrice = product.SalePrice;
        }

        private void ValidateItem(OrderItem item, Product product, bool isApproved)
        {
            var errs = new List<string>();

            if (product == null)
                errs.Add($"invalid product id {item.ProductId}");

            if (isApproved && (product != null) && (item.Quantity > product.Stock))
                errs.Add($"insufficient stock for product {product.Description}");

            ThrowExceptionIfNeed(errs);
        }

        private async Task ValidateBasicData(Order order)
        {
            var errs = new List<string>();
            var customer = await _customers.GetByKey(order.CustomerId);

            if (customer == null)
                errs.Add($"invalid customer id {order.CustomerId}");

            if(order.Id.Equals(0) && order.IsCanceled())
                errs.Add("invalid status for new order");

            if ((order.Id > 0) && order.IsFinished())
                errs.Add("cannot change finished order");

            ThrowExceptionIfNeed(errs);
        }

        private void ThrowExceptionIfNeed(List<string> errs)
        {
            if (errs.Count > 0) throw new System.Exception(string.Concat("Errors Occurred: ", string.Join(", ", errs)));
        }
    }
}
