using DataEntities;
using Microsoft.EntityFrameworkCore;
using Products.Data;

namespace Products.Endpoints;

/// <summary>
/// Provides extension methods to map Product-related HTTP endpoints to an <see cref="IEndpointRouteBuilder"/>.
/// </summary>
/// <remarks>
/// This static class centralizes registration of minimal API endpoints for CRUD operations against the
/// <see cref="Product"/> entity using a <see cref="ProductDataContext"/>. Endpoints are grouped under the
/// "/api/Product" route prefix and use EF Core for data access. This class is intended to be called during
/// application startup to configure the product API surface.
/// </remarks>
public static class ProductEndpoints
{
    /// <summary>
    /// Maps a set of HTTP endpoints for product management onto the provided route builder.
    /// </summary>
    /// <param name="routes">The application's <see cref="IEndpointRouteBuilder"/> used to register routes.</param>
    /// <remarks>
    /// The following endpoints are registered within a route group rooted at "/api/Product":
    /// - GET "/"           : Returns all products (200 OK).
    /// - GET "/{productId}": Returns a single product by id (200 OK) or 404 Not Found.
    /// - PUT "/{id}"       : Updates a product with provided id (204 No Content) or 404 Not Found.
    /// - POST "/"          : Creates a new product (201 Created).
    /// - DELETE "/{id}"    : Deletes a product by id (200 OK) or 404 Not Found.
    ///
    /// Each handler receives a <see cref="ProductDataContext"/> from DI and performs EF Core queries/commands.
    /// Use this method during application startup to ensure the product API endpoints are available.
    /// </remarks>
    public static void MapProductEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/Product");

        group.MapGet("/", async (ProductDataContext db) =>
        {
            return await db.Product.ToListAsync();
        })
        .WithName("GetAllProducts")
        .Produces<List<Product>>(StatusCodes.Status200OK);

        group.MapGet("/{productId}", async (int productId, ProductDataContext db) =>
        {
            return await db.Product.AsNoTracking()
                .FirstOrDefaultAsync(model => model.Id == productId)
                is Product model
                    ? Results.Ok(model)
                    : Results.NotFound();
        })
        .WithName("GetProductById")
        .Produces<Product>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        group.MapPut("/{id}", async (int id, Product product, ProductDataContext db) =>
        {
            var affected = await db.Product
                .Where(model => model.Id == id)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(m => m.Name, product.Name)
                    .SetProperty(m => m.Description, product.Description)
                    .SetProperty(m => m.Price, product.Price)
                    .SetProperty(m => m.ImageUrl, product.ImageUrl)
                );

            return affected == 1 ? Results.NoContent() : Results.NotFound();
        })
        .WithName("UpdateProduct")
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status204NoContent);

        group.MapPost("/", async (Product product, ProductDataContext db) =>
        {
            db.Product.Add(product);
            await db.SaveChangesAsync();
            return Results.Created($"/api/Product/{product.Id}", product);
        })
        .WithName("CreateProduct")
        .Produces<Product>(StatusCodes.Status201Created);

        group.MapDelete("/{id}", async (int id, ProductDataContext db) =>
        {
            var affected = await db.Product
                .Where(model => model.Id == id)
                .ExecuteDeleteAsync();

            return affected == 1 ? Results.Ok() : Results.NotFound();
        })
        .WithName("DeleteProduct")
        .Produces<Product>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);
    }
}
