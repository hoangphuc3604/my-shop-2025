using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyShop.Contracts;
using MyShop.Data.Models;
using MyShop.Services.GraphQL;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyShop.Services
{
    public class PromotionService : IPromotionService
    {
        private readonly GraphQLClient _graphQLClient;
        private readonly IAuthorizationService _authorizationService;

        public PromotionService(GraphQLClient graphQLClient, IAuthorizationService authorizationService)
        {
            _graphQLClient = graphQLClient;
            _authorizationService = authorizationService;
        }

        public async Task<List<Promotion>> GetPromotionsAsync(
            int page,
            int pageSize,
            string? search,
            bool? isActive,
            string? token)
        {
            try
            {
                var queryBuilder = new List<string>
                {
                    $"page: {page}",
                    $"limit: {pageSize}"
                };

                if (!string.IsNullOrEmpty(search))
                {
                    queryBuilder.Add($"search: \"{search}\"");
                }

                var paramsString = string.Join(", ", queryBuilder);

                var query = $@"
                    query {{
                        promotions(params: {{ {paramsString} }}) {{
                            items {{
                                promotionId
                                code
                                description
                                discountType
                                discountValue
                                appliesTo
                                appliesToIds
                                startAt
                                endAt
                                isActive
                                createdAt
                            }}
                            pagination {{
                                currentPage
                                hasNextPage
                                hasPrevPage
                                limit
                                totalCount
                                totalPages
                            }}
                        }}
                    }}";

                var response = await _graphQLClient.QueryAsync<PaginatedPromotionsResponse>(query, null, token);

                if (response?.Promotions?.Items == null)
                {
                    return new List<Promotion>();
                }

                return response.Promotions.Items.Select(MapToPromotion).ToList();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PROMOTION_SERVICE] Error getting promotions: {ex.Message}");
                throw;
            }
        }

        public async Task<Promotion?> GetPromotionByIdAsync(int promotionId, string? token)
        {
            try
            {
                var query = $@"
                    query {{
                        promotion(id: ""{promotionId}"") {{
                            promotionId
                            code
                            description
                            discountType
                            discountValue
                            appliesTo
                            appliesToIds
                            startAt
                            endAt
                            isActive
                            createdAt
                        }}
                    }}";

                var response = await _graphQLClient.QueryAsync<PromotionDetailResponse>(query, null, token);

                if (response?.Promotion == null)
                {
                    return null;
                }

                return MapToPromotion(response.Promotion);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PROMOTION_SERVICE] Error getting promotion by ID: {ex.Message}");
                throw;
            }
        }

        public async Task<int> GetTotalPromotionCountAsync(
            string? search,
            bool? isActive,
            string? token)
        {
            try
            {
                var queryBuilder = new List<string>();

                if (!string.IsNullOrEmpty(search))
                {
                    queryBuilder.Add($"search: \"{search}\"");
                }

                var paramsString = string.Join(", ", queryBuilder);
                if (!string.IsNullOrEmpty(paramsString))
                {
                    paramsString = $"(params: {{ {paramsString} }})";
                }

                var query = $@"
                    query {{
                        promotions{paramsString} {{
                            pagination {{
                                totalCount
                            }}
                        }}
                    }}";

                var response = await _graphQLClient.QueryAsync<PaginatedPromotionsResponse>(query, null, token);

                return response?.Promotions?.Pagination?.TotalCount ?? 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PROMOTION_SERVICE] Error getting total promotion count: {ex.Message}");
                throw;
            }
        }

        public async Task<Promotion?> CreatePromotionAsync(
            string code,
            string description,
            PromotionType discountType,
            int discountValue,
            AppliesTo appliesTo,
            int[]? appliesToIds,
            DateTime? startAt,
            DateTime? endAt,
            string? token)
        {
            try
            {
                var inputBuilder = new List<string>
                {
                    $"code: \"{code}\"",
                    $"description: \"{description}\"",
                    $"discountType: {discountType}",
                    $"discountValue: {discountValue}",
                    $"appliesTo: {appliesTo}"
                };

                if (appliesToIds != null && appliesToIds.Length > 0)
                {
                    inputBuilder.Add($"appliesToIds: [{string.Join(", ", appliesToIds)}]");
                }

                if (startAt.HasValue)
                {
                    inputBuilder.Add($"startAt: \"{startAt.Value.ToString("O")}\"");
                }

                if (endAt.HasValue)
                {
                    inputBuilder.Add($"endAt: \"{endAt.Value.ToString("O")}\"");
                }

                var inputString = string.Join(", ", inputBuilder);

                var mutation = $@"
                    mutation {{
                        createPromotion(input: {{ {inputString} }}) {{
                            promotionId
                            code
                            description
                            discountType
                            discountValue
                            appliesTo
                            appliesToIds
                            startAt
                            endAt
                            isActive
                            createdAt
                        }}
                    }}";

                var response = await _graphQLClient.MutateAsync<CreatePromotionResponse>(mutation, null, token);

                if (response?.CreatePromotion == null)
                {
                    return null;
                }

                return MapToPromotion(response.CreatePromotion);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PROMOTION_SERVICE] Error creating promotion: {ex.Message}");
                throw;
            }
        }

        public async Task<Promotion?> UpdatePromotionAsync(
            int promotionId,
            string? code,
            string? description,
            PromotionType? discountType,
            int? discountValue,
            AppliesTo? appliesTo,
            int[]? appliesToIds,
            DateTime? startAt,
            DateTime? endAt,
            bool? isActive,
            string? token)
        {
            try
            {
                var inputBuilder = new List<string>();

                if (!string.IsNullOrEmpty(code))
                {
                    inputBuilder.Add($"code: \"{code}\"");
                }

                if (!string.IsNullOrEmpty(description))
                {
                    inputBuilder.Add($"description: \"{description}\"");
                }

                if (discountType.HasValue)
                {
                    inputBuilder.Add($"discountType: {discountType.Value}");
                }

                if (discountValue.HasValue)
                {
                    inputBuilder.Add($"discountValue: {discountValue.Value}");
                }

                if (appliesTo.HasValue)
                {
                    inputBuilder.Add($"appliesTo: {appliesTo.Value}");
                }

                if (appliesToIds != null)
                {
                    inputBuilder.Add($"appliesToIds: [{string.Join(", ", appliesToIds)}]");
                }

                if (startAt.HasValue)
                {
                    inputBuilder.Add($"startAt: \"{startAt.Value.ToString("O")}\"");
                }
                else
                {
                    inputBuilder.Add("startAt: null");
                }

                if (endAt.HasValue)
                {
                    inputBuilder.Add($"endAt: \"{endAt.Value.ToString("O")}\"");
                }
                else
                {
                    inputBuilder.Add("endAt: null");
                }

                if (isActive.HasValue)
                {
                    inputBuilder.Add($"isActive: {isActive.Value.ToString().ToLower()}");
                }

                var inputString = string.Join(", ", inputBuilder);

                var mutation = $@"
                    mutation {{
                        updatePromotion(id: ""{promotionId}"", input: {{ {inputString} }}) {{
                            promotionId
                            code
                            description
                            discountType
                            discountValue
                            appliesTo
                            appliesToIds
                            startAt
                            endAt
                            isActive
                            createdAt
                        }}
                    }}";

                var response = await _graphQLClient.MutateAsync<UpdatePromotionResponse>(mutation, null, token);

                if (response?.UpdatePromotion == null)
                {
                    return null;
                }

                return MapToPromotion(response.UpdatePromotion);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PROMOTION_SERVICE] Error updating promotion: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> DeletePromotionAsync(int promotionId, string? token)
        {
            try
            {
                var mutation = $@"
                    mutation {{
                        deletePromotion(id: ""{promotionId}"")
                    }}";

                var response = await _graphQLClient.MutateAsync<DeletePromotionResponse>(mutation, null, token);

                return response?.DeletePromotion ?? false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PROMOTION_SERVICE] Error deleting promotion: {ex.Message}");
                throw;
            }
        }

        public async Task<PromotionCalculationResult?> ApplyPromotionAsync(
            string code,
            int orderTotal,
            List<OrderItemInput> orderItems,
            string? token)
        {
            // Note: Backend doesn't have applyPromotion mutation yet
            // This is a placeholder for future implementation
            // For now, return null to indicate not implemented
            return null;
        }

        public async Task<List<Promotion>> GetActivePromotionsAsync(string? token)
        {
            try
            {
                // For SALE role, get active promotions without requiring MANAGE_SYSTEM permission
                // This would need a separate query in backend that doesn't require high permissions
                var query = $@"
                    query {{
                        promotions(params: {{ isActive: true, limit: 100 }}) {{
                            items {{
                                promotionId
                                code
                                description
                                discountType
                                discountValue
                                appliesTo
                                appliesToIds
                                startAt
                                endAt
                                isActive
                                createdAt
                            }}
                        }}
                    }}";

                var response = await _graphQLClient.QueryAsync<PaginatedPromotionsResponse>(query, null, token);

                if (response?.Promotions?.Items == null)
                {
                    return new List<Promotion>();
                }

                return response.Promotions.Items
                    .Where(p => IsPromotionActive(p))
                    .Select(MapToPromotion)
                    .ToList();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PROMOTION_SERVICE] Error getting active promotions: {ex.Message}");
                throw;
            }
        }

        private bool IsPromotionActive(PromotionData promotion)
        {
            if (!promotion.IsActive)
                return false;

            var now = DateTime.UtcNow;

            var startDate = ParseDateString(promotion.StartAt);
            if (startDate.HasValue && now < startDate.Value)
            {
                return false;
            }

            var endDate = ParseDateString(promotion.EndAt);
            if (endDate.HasValue && now > endDate.Value)
            {
                return false;
            }

            return true;
        }

        private Promotion MapToPromotion(PromotionData data)
        {
            return new Promotion
            {
                PromotionId = data.PromotionId,
                Code = data.Code ?? string.Empty,
                Description = data.Description ?? string.Empty,
                DiscountType = Enum.Parse<PromotionType>(data.DiscountType ?? "PERCENTAGE"),
                DiscountValue = data.DiscountValue,
                AppliesTo = Enum.Parse<AppliesTo>(data.AppliesTo ?? "ALL"),
                AppliesToIds = data.AppliesToIds,
                StartAt = ParseDateString(data.StartAt),
                EndAt = ParseDateString(data.EndAt),
                IsActive = data.IsActive,
                CreatedAt = ParseDateString(data.CreatedAt) ?? DateTime.MinValue
            };
        }

        private DateTime? ParseDateString(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return null;

            if (DateTime.TryParse(value, out var dt))
                return dt;

            if (long.TryParse(value, out var ms))
            {
                try
                {
                    return DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime;
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }
    }
}
