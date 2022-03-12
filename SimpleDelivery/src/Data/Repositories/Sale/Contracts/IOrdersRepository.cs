﻿using Data.Entities.Sale;
using Data.Repositories.Contracts;

namespace Data.Repositories.Sale.Contracts
{
    public interface IOrdersRepository : IBasicRepository<Order>
    {
    }
}
