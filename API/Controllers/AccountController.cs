using API.DTOs;
using API.Entities;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    public class AccountController: BaseApiController
    {
        private readonly ITokenService _token;
        private readonly IMapper _mapper;
        private readonly UserManager<AppUser> _userManager;

        public AccountController(UserManager<AppUser> userManager, ITokenService token, IMapper mapper)
        {
            _userManager = userManager;
            _mapper = mapper;
            _token = token;
        }

        [HttpPost("register")]
        public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto)
        {
           if(await IsExists(registerDto.Username)) return Unauthorized("Username is taken");

           var user = _mapper.Map<AppUser>(registerDto);

             user.UserName = registerDto.Username.ToLower();;

           var result = await _userManager.CreateAsync(user, registerDto.Password);

           if(!result.Succeeded) return BadRequest(result.Errors);

           var roleResult = await _userManager.AddToRoleAsync(user, "Member");

           if(!roleResult.Succeeded)  return BadRequest(roleResult.Errors);

           return new UserDto
           {
             UserName = user.UserName,
             Token = await _token.CreateToken(user),
             KnownAs = user.KnownAs,
             Gender = user.Gender
           };
        }

        [HttpPost("login")]
        public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
        {
            var user = await _userManager.Users.Include(q => q.Photos).SingleOrDefaultAsync(q => q.UserName == loginDto.Username.ToLower());
            if(user is null) return Unauthorized("Invalid Username");

            var result = await _userManager.CheckPasswordAsync(user, loginDto.Password);

            if(!result) return Unauthorized("Invalid password");

            return new UserDto
            {
                UserName = loginDto.Username,
                Token = await _token.CreateToken(user),
                PhotoUrl = user.Photos.FirstOrDefault(q => q.IsMain)?.Url,
                KnownAs = user.KnownAs,
                Gender = user.Gender
            };
        }

        private async Task<bool> IsExists(string username)
        {
            return await _userManager.Users.AnyAsync(q => q.UserName == username.ToLower());
        }
    }
}