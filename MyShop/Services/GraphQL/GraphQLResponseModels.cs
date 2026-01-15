using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyShop.Services.GraphQL
{
    public class LoginResponse
    {
        [JsonPropertyName("login")]
        public LoginResult? Login { get; set; }
    }

    public class LoginResult
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("token")]
        public string? Token { get; set; }

        [JsonPropertyName("user")]
        public UserData? User { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }

    public class UserData
    {
        [JsonPropertyName("userId")]
        [JsonConverter(typeof(StringToIntConverter))]
        public int UserId { get; set; }

        [JsonPropertyName("username")]
        public string? Username { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("createdAt")]
        public string? CreatedAt { get; set; }

        [JsonPropertyName("lastLogin")]
        public string? LastLogin { get; set; }

        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; }
    }

    // ============ PAGINATED ORDERS RESPONSE ============
    public class PaginatedOrdersResponse
    {
        [JsonPropertyName("orders")]
        public PaginatedOrdersResult? Orders { get; set; }
    }

    public class PaginatedOrdersResult
    {
        [JsonPropertyName("items")]
        public OrderData[]? Items { get; set; }

        [JsonPropertyName("pagination")]
        public PaginationInfo? Pagination { get; set; }
    }

    public class PaginationInfo
    {
        [JsonPropertyName("currentPage")]
        [JsonConverter(typeof(StringToIntConverter))]
        public int CurrentPage { get; set; }

        [JsonPropertyName("hasNextPage")]
        public bool HasNextPage { get; set; }

        [JsonPropertyName("hasPrevPage")]
        public bool HasPrevPage { get; set; }

        [JsonPropertyName("limit")]
        [JsonConverter(typeof(StringToIntConverter))]
        public int Limit { get; set; }

        [JsonPropertyName("totalCount")]
        [JsonConverter(typeof(StringToIntConverter))]
        public int TotalCount { get; set; }

        [JsonPropertyName("totalPages")]
        [JsonConverter(typeof(StringToIntConverter))]
        public int TotalPages { get; set; }
    }

    // ============ NON-PAGINATED ORDERS RESPONSE (for backward compatibility) ============
    public class OrdersResponse
    {
        [JsonPropertyName("orders")]
        public OrderData[]? Orders { get; set; }
    }

    public class OrderData
    {
        [JsonPropertyName("orderId")]
        [JsonConverter(typeof(StringToIntConverter))]
        public int OrderId { get; set; }

        [JsonPropertyName("createdTime")]
        public string? CreatedTime { get; set; }

        [JsonPropertyName("finalPrice")]
        [JsonConverter(typeof(StringToIntConverter))]
        public int FinalPrice { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("orderItems")]
        public OrderItemData[]? OrderItems { get; set; }
    }

    /// <summary>
    /// Order item with product data
    /// </summary>
    public class OrderItemData
    {
        [JsonPropertyName("orderItemId")]
        [JsonConverter(typeof(StringToIntConverter))]
        public int OrderItemId { get; set; }

        [JsonPropertyName("quantity")]
        [JsonConverter(typeof(StringToIntConverter))]
        public int Quantity { get; set; }

        [JsonPropertyName("unitSalePrice")]
        [JsonConverter(typeof(StringToFloatConverter))]
        public float UnitSalePrice { get; set; }

        [JsonPropertyName("totalPrice")]
        [JsonConverter(typeof(StringToIntConverter))]
        public int TotalPrice { get; set; }

        [JsonPropertyName("product")]
        public ProductData? Product { get; set; }
    }

    public class ProductData
    {
        [JsonPropertyName("productId")]
        [JsonConverter(typeof(StringToIntConverter))]
        public int ProductId { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("sku")]
        public string? Sku { get; set; }

        [JsonPropertyName("importPrice")]
        [JsonConverter(typeof(StringToIntConverter))]
        public int ImportPrice { get; set; }

        [JsonPropertyName("count")]
        [JsonConverter(typeof(StringToIntConverter))]
        public int Count { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("images")]
        public ProductImageData[]? Images { get; set; }

        [JsonPropertyName("category")]
        public CategoryData? Category { get; set; }
    }

    public class ProductImageData
    {
        [JsonPropertyName("productImageId")]
        [JsonConverter(typeof(StringToIntConverter))]
        public int ProductImageId { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("altText")]
        public string? AltText { get; set; }

        [JsonPropertyName("position")]
        [JsonConverter(typeof(StringToIntConverter))]
        public int Position { get; set; }

        [JsonPropertyName("isPrimary")]
        public bool IsPrimary { get; set; }
    }

    public class CategoryData
    {
        [JsonPropertyName("categoryId")]
        [JsonConverter(typeof(StringToIntConverter))]
        public int CategoryId { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }

    public class ProductsResponse
    {
        [JsonPropertyName("products")]
        public ProductData[]? Products { get; set; }
    }

    // ============ PAGINATED PRODUCTS RESPONSE ============
    public class PaginatedProductsResponse
    {
        [JsonPropertyName("products")]
        public PaginatedProductsResult? Products { get; set; }
    }

    public class PaginatedProductsResult
    {
        [JsonPropertyName("items")]
        public ProductData[]? Items { get; set; }

        [JsonPropertyName("pagination")]
        public PaginationInfo? Pagination { get; set; }
    }

    // ============ CATEGORIES RESPONSES ============
    public class CategoriesResponse
    {
        [JsonPropertyName("categories")]
        public CategoryData[]? Categories { get; set; }
    }

    public class PaginatedCategoriesResponse
    {
        [JsonPropertyName("categories")]
        public PaginatedCategoriesResult? Categories { get; set; }
    }

    public class PaginatedCategoriesResult
    {
        [JsonPropertyName("items")]
        public CategoryData[]? Items { get; set; }

        [JsonPropertyName("pagination")]
        public PaginationInfo? Pagination { get; set; }
    }

    public class CategoryDetailResponse
    {
        [JsonPropertyName("category")]
        public CategoryData? Category { get; set; }
    }

    public class OrderCountResponse
    {
        [JsonPropertyName("orderCount")]
        [JsonConverter(typeof(StringToIntConverter))]
        public int OrderCount { get; set; }
    }

    public class CreateOrderResponse
    {
        [JsonPropertyName("addOrder")]
        public OrderData? AddOrder { get; set; }
    }

    public class UpdateOrderResponse
    {
        [JsonPropertyName("updateOrder")]
        public UpdateOrderData? UpdateOrder { get; set; }
    }

    /// <summary>
    /// Simplified order data for update response (doesn't include createdTime, orderId)
    /// </summary>
    public class UpdateOrderData
    {
        [JsonPropertyName("finalPrice")]
        [JsonConverter(typeof(StringToIntConverter))]
        public int FinalPrice { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("orderItems")]
        public OrderItemData[]? OrderItems { get; set; }

        // These properties are needed to map to Order model
        // Set them based on the context or keep them as optional
        [JsonIgnore]
        public int OrderId { get; set; }

        [JsonIgnore]
        public string? CreatedTime { get; set; }
    }

    public class DeleteOrderResponse
    {
        [JsonPropertyName("deleteOrder")]
        public DeleteResult? DeleteOrder { get; set; }
    }

    public class DeleteOrderSimpleResponse
    {
        [JsonPropertyName("deleteOrder")]
        public bool DeleteOrder { get; set; }
    }

    public class DeleteResult
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }

    /// <summary>
    /// Custom converter to handle string values coming from backend and convert them to int
    /// </summary>
    public class StringToIntConverter : JsonConverter<int>
    {
        public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.String:
                    string? stringValue = reader.GetString();
                    if (int.TryParse(stringValue, out int intValue))
                    {
                        return intValue;
                    }
                    throw new JsonException($"Unable to convert \"{stringValue}\" to int");

                case JsonTokenType.Number:
                    if (reader.TryGetInt32(out int number))
                    {
                        return number;
                    }
                    throw new JsonException("Unable to convert number to int");

                default:
                    throw new JsonException($"Unexpected token type {reader.TokenType} when parsing int");
            }
        }

        public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value);
        }
    }

    /// <summary>
    /// Custom converter to handle string values coming from backend and convert them to float
    /// </summary>
    public class StringToFloatConverter : JsonConverter<float>
    {
        public override float Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.String:
                    string? stringValue = reader.GetString();
                    if (float.TryParse(stringValue, out float floatValue))
                    {
                        return floatValue;
                    }
                    throw new JsonException($"Unable to convert \"{stringValue}\" to float");

                case JsonTokenType.Number:
                    if (reader.TryGetSingle(out float number))
                    {
                        return number;
                    }
                    throw new JsonException("Unable to convert number to float");

                default:
                    throw new JsonException($"Unexpected token type {reader.TokenType} when parsing float");
            }
        }

        public override void Write(Utf8JsonWriter writer, float value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value);
        }
    }

    /// <summary>
    /// Custom converter to handle Unix timestamp (milliseconds) and convert to ISO 8601 string
    /// </summary>
    public class UnixTimestampConverter : JsonConverter<string>
    {
        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.String:
                    string? stringValue = reader.GetString();
                    if (string.IsNullOrEmpty(stringValue))
                        return stringValue;

                    // Try to parse as Unix timestamp in milliseconds
                    if (long.TryParse(stringValue, out long unixMs))
                    {
                        var dateTime = UnixEpoch.AddMilliseconds(unixMs);
                        return dateTime.ToString("o");  // ISO 8601 format
                    }

                    // If not a timestamp, return as-is (might be ISO format already)
                    return stringValue;

                case JsonTokenType.Number:
                    if (reader.TryGetInt64(out long number))
                    {
                        var dateTime = UnixEpoch.AddMilliseconds(number);
                        return dateTime.ToString("o");  // ISO 8601 format
                    }
                    throw new JsonException("Unable to convert number to timestamp");

                default:
                    throw new JsonException($"Unexpected token type {reader.TokenType} when parsing timestamp");
            }
        }

        public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
            }
            else
            {
                writer.WriteStringValue(value);
            }
        }
    }

    // ============ MUTATION RESPONSES ============

    public class CreateCategoryResponse
    {
        [JsonPropertyName("createCategory")]
        public CategoryData? CreateCategory { get; set; }
    }

    public class DeleteCategoryResponse
    {
        [JsonPropertyName("deleteCategory")]
        public bool DeleteCategory { get; set; }
    }

    public class CreateProductResponse
    {
        [JsonPropertyName("createProduct")]
        public ProductData? CreateProduct { get; set; }
    }

    public class UpdateProductResponse
    {
        [JsonPropertyName("updateProduct")]
        public ProductData? UpdateProduct { get; set; }
    }

    public class DeleteProductResponse
    {
        [JsonPropertyName("deleteProduct")]
        public bool DeleteProduct { get; set; }
    }

    public class BulkCreateProductsResponse
    {
        [JsonPropertyName("bulkCreateProducts")]
        public BulkUploadResultData? BulkCreateProducts { get; set; }
    }

    public class BulkUploadResultData
    {
        [JsonPropertyName("createdCount")]
        public int CreatedCount { get; set; }

        [JsonPropertyName("failedCount")]
        public int FailedCount { get; set; }

        [JsonPropertyName("errors")]
        public BulkRowErrorData[]? Errors { get; set; }
    }

    public class BulkRowErrorData
    {
        [JsonPropertyName("row")]
        public int Row { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("field")]
        public string? Field { get; set; }
    }
}