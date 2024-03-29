﻿using Data;
using Data.Entities.Catalog;
using Data.Repositories.Catalog.Contracts;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WebApi.Controllers
{
    public class ProductsController : BaseController
    {
        private readonly SimpleDeliveryContext _context;
        private readonly IProductsRepository _products;

        public ProductsController(SimpleDeliveryContext context, IProductsRepository products)
        {
            _context = context;
            _products = products;
        }

        [ProducesResponseType(typeof(IList<Product>), statusCode: 200)]
        [HttpGet]
        public async Task<IActionResult> Get(string description = null)
        {
            try
            {
                IList<Product> products;

                if (description != null)
                    products = await _products.GetAllByDescription(description);
                else
                    products = await _products.GetAll();

                return Ok(products);
            }
            catch (Exception ex)
            {
                return Error(ex);
            }
        }

        [ProducesResponseType(typeof(Product), statusCode: 200)]
        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            try
            {
                var product = await _products.GetByKey(id);
                if (product == null)
                    return NotFound();

                return Ok(product);
            }
            catch(Exception ex)
            {
                return Error(ex);
            }
        }

        [ProducesResponseType(statusCode: 204)]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _products.DeleteByKey(id);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch(Exception ex)
            {
                return Error(ex);
            }
        }

        [ProducesResponseType(statusCode: 204)]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Put(int id, [FromBody] Product product)
        {
            try
            {
                if (id != product.Id)
                    return BadRequest("Entity mismatch. Id provided on url is different of Id provided on body");

                if (!TryValidateModel(product))
                    return BadRequest("Invalid data provided");

                await _products.Update(product);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return Error(ex);
            }
        }

        [ProducesResponseType(typeof(Product), statusCode: 200)]
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Product product)
        {
            try
            {
                if (!TryValidateModel(product))
                    return BadRequest("Invalid data provided");

                await _products.Add(product);
                await _context.SaveChangesAsync();
                return Ok(product);
            }
            catch (Exception ex)
            {
                return Error(ex);
            }
        }
    }
}
