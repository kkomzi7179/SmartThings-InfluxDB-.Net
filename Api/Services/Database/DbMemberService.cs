
using Microsoft.EntityFrameworkCore;

using Api.Database;
using Api.Database.Models;

namespace Api.Services.Database;
public interface IDbMemberService
    {
        Task<bool> ExistByEmail(string userEmail);
        Task<Member?> GetById(long id);
        Task<Member?> GetByEmail(string userEmail);
        Task<IEnumerable<Member>> GetAll();
        Task<int> Insert(Member member);
        Task<int> Update(Member member);
        Task<int> Delete(long id);
    }
    public class DbMemberService : IDbMemberService {
        private readonly MyDbContext _dbContext;

        public DbMemberService(MyDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<bool> ExistByEmail(string userEmail)
        {
            return await _dbContext.Members.AnyAsync(o => o.UserEmail == userEmail);
        }
        public async Task<Member?> GetById(long id)
        {
            return await _dbContext.Members.FindAsync(id);
        }
        public async Task<Member?> GetByEmail(string userEmail)
        {
            return await _dbContext.Members.FirstOrDefaultAsync(o => o.UserEmail == userEmail);
        }
        public async Task<IEnumerable<Member>> GetAll()
        {
            return await _dbContext.Members.ToListAsync();
        }
        public async Task<int> Insert(Member member)
        {
            var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                await _dbContext.Members.AddAsync(member);

			    var rtn = await _dbContext.SaveChangesAsync();

                await transaction.CommitAsync();

                return rtn;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return 0;
            }
        }

        public async Task<int> Update(Member member)
        {
            try
            {
                _dbContext.Members.Update(member);
                return await _dbContext.SaveChangesAsync();
            }
            catch (Exception)
            {
                return 0;
            }
        }
        public async Task<int> Delete(long id)
        {
            var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                _dbContext.Members.Remove(
                    new Member
                    {
                        ID = id
                    }
                );

                var rtn = await _dbContext.SaveChangesAsync();

                await transaction.CommitAsync();

                return rtn;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return 0;
            }
        }
    }
