﻿using Data;
using Data.Entities.Store;
using Data.Repositories.Store.Contracts;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WebApi.Controllers
{
    public class StoresController : BaseController
    {
        private readonly SimpleDeliveryContext _context;
        private readonly IStoresRepository _stores;

        public StoresController(SimpleDeliveryContext context, IStoresRepository stores)
        {
            _context = context;
            _stores = stores;
        }

        [ProducesResponseType(typeof(IList<StoreInfo>), statusCode: 200)]
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try
            {
                var stores = await _stores.GetAll();
                return Ok(stores);
            }
            catch(Exception ex)
            {
                return Error(ex);
            }
        }

        [ProducesResponseType(typeof(StoreInfo), statusCode: 200)]
        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            try
            {
                var store = await _stores.GetByKey(id);
                if (store == null)
                    return NotFound();

                return Ok(store);
            }
            catch(Exception ex)
            {
                return Error(ex);
            }
        }

        [ProducesResponseType(statusCode: 204)]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Put(int id, [FromBody] StoreInfo store)
        {
            try
            {
                if (id != store.Id)
                    return BadRequest("Entity mismatch. Id provided on url is different of Id provided on body");

                if (!TryValidateModel(store))
                    return BadRequest("Invalid data provided");

                await _stores.Update(store);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return Error(ex);
            }
        }
    }
}
