using Employee_Assignment.Application.Interfaces.Repositories;
using Employee_Assignment.Domain.Entities;
using Employee_Assignment.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Employee_Assignment.Infrastructure.Repositories
{
    public class LeaveTypeRepository : ILeaveTypeRepository
    {
        private readonly ApplicationDbContext _context;

        public LeaveTypeRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<LeaveType>> GetAllAsync()
        {
            return await _context.LeaveTypes
                .Where(lt => lt.IsActive)
                .OrderBy(lt => lt.Name)
                .ToListAsync();
        }

        public async Task<LeaveType?> GetByIdAsync(int id)
        {
            return await _context.LeaveTypes.FindAsync(id);
        }
    }
}