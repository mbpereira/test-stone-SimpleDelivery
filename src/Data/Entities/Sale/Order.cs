﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace Data.Entities.Sale
{
    public class Order
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public DateTime Date { get; set; }
        [Required]
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        [Required]
        public decimal ShipmentValue { get; set; }
        [Required, DefaultValue(OrderStatus.Received)]
        public OrderStatus Status { get; set; }
        public Customer Customer { get; set; }
        [Required]
        public int CustomerId { get; set; }
        [Required]
        public IList<OrderItem> Itens { get; set; }
        [NotMapped]
        public decimal GrossTotal => Itens.Sum(i => i.GrossTotal);
        [NotMapped]
        public decimal NetTotal => Itens.Sum(i => i.NetTotal);
        [NotMapped]
        public decimal TotalDiscount => Itens.Sum(i => i.TotalDiscount);
        [NotMapped]
        public decimal TotalCost => Itens.Sum(i => i.TotalCost);
        [NotMapped]
        public decimal Profit => Math.Round(NetTotal - TotalCost, decimals: 2);
        [NotMapped]
        public decimal PercProfit => NetTotal > 0 
            ? Math.Round(Profit / NetTotal * 100, decimals: 2) 
            : 0;
        public bool IsApproved() => new[] { OrderStatus.Approved, OrderStatus.Preparing, OrderStatus.Delivered }.Contains(Status);
        public bool IsOpen() => Status.Equals(OrderStatus.Received);
        public bool IsCanceled() => Status.Equals(OrderStatus.Canceled);
        public bool IsFinished() => new[] { OrderStatus.Canceled, OrderStatus.Delivered }.Contains(Status);

        public Order Clone()
        {
            return (Order) MemberwiseClone();
        }
    }
}
