using Microsoft.AspNetCore.Mvc;
using ProductApi.Data;
using ProductApi.Models;

namespace ProductApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductRepository _repo;

    public ProductsController(IProductRepository repo) => _repo = repo;

    /// <summary>查全部商品</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Product>>> GetAll()
        => Ok(await _repo.GetAllAsync());

    /// <summary>依 Id 查一筆</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Product>> GetById(int id)
    {
        var product = await _repo.GetByIdAsync(id);
        return product is null ? NotFound() : Ok(product);
    }

    /// <summary>新增</summary>
    [HttpPost]
    public async Task<ActionResult<Product>> Create(ProductRequest req)
    {
        var created = await _repo.CreateAsync(req);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>修改</summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<Product>> Update(int id, ProductRequest req)
    {
        var updated = await _repo.UpdateAsync(id, req);
        return updated is null ? NotFound() : Ok(updated);
    }

    /// <summary>刪除</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
        => await _repo.DeleteAsync(id) ? NoContent() : NotFound();
}
