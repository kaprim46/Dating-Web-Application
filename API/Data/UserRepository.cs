using API.DTOs;
using API.Entities;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _db;
        private readonly IMapper _mapper;

        public UserRepository(AppDbContext db, IMapper mapper)
        {
            _mapper = mapper;
            _db = db;
        }

        public async Task<MemberDto> GetMemberByUsernameAsync(string username)
        {
            return await _db.Users
                .Where(q => q.UserName == username)
                .ProjectTo<MemberDto>(_mapper.ConfigurationProvider)
                .SingleOrDefaultAsync();
        }

        public async Task<PagedList<MemberDto>> GetMembersAsync(UserParams userParams)
        {
            var query = _db.Users.AsQueryable();
            query = query.Where(q => q.UserName != userParams.CurrentUsername);
            query = query.Where(q => q.Gender == userParams.Gender);

            var minDob = DateOnly.FromDateTime(DateTime.Today.AddYears(-userParams.MaxAge - 1));
            var maxDob = DateOnly.FromDateTime(DateTime.Today.AddYears(-userParams.MinAge));

            query = query.Where(q => q.DateOfBirth >= minDob && q.DateOfBirth <= maxDob);

            query = userParams.OrderBy switch
            {
                "created" => query.OrderByDescending(q => q.Created),
                _ => query.OrderByDescending(q => q.LastActive)
            };

            return await PagedList<MemberDto>.CreateAsync(
                query.AsNoTracking().ProjectTo<MemberDto>(_mapper.ConfigurationProvider),
                userParams.PageNumber, 
                userParams.PageSize);
        }

        public async Task<AppUser> GetUserByIdAsync(int id)
        {
            return await _db.Users.FindAsync(id);
        }

        public async Task<AppUser> GetUserByUsernameAsync(string username)
        {
            return await _db.Users.Include(q => q.Photos).SingleOrDefaultAsync(q => q.UserName == username);
        }

        public async Task<string> GetUserGender(string username)
        {
            return await _db.Users.Where(q => q.UserName == username)
                                  .Select(q => q.Gender).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<AppUser>> GetUsersAsync()
        {
            return await _db.Users.Include(q => q.Photos).ToListAsync();
        }

        public void Update(AppUser user)
        {
            _db.Entry(user).State = EntityState.Modified;
        }
    }
}