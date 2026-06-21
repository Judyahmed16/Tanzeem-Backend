
using Tanzeem.Shared.Dtos;
using Microsoft.EntityFrameworkCore;
namespace Tanzeem.Shared
{
    public static class PaginationExtension
    {
        public static async Task<PaginationResponseDto<T>> ToPaginatedResponseAsync<T>(
        this IQueryable<T> source, int page, int pageSize)
        {
            var totalCount = await source.CountAsync();

            var data = await source
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PaginationResponseDto<T>
            {
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                Data = data
            };
        }

        public static PaginationResponseDto<T> ToPaginatedResponse<T>(
        this IEnumerable<T> source, int page, int pageSize)
        {
            var list = source.ToList();
            return new PaginationResponseDto<T>
            {
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = list.Count,
                Data = list.Skip((page - 1) * pageSize).Take(pageSize).ToList()
            };
        }
    }
}
