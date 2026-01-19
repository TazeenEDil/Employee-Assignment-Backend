
using System.ComponentModel.DataAnnotations;

namespace Employee_Assignment.Application.DTOs.Common
{
    public class PaginationRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}

