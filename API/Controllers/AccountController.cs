using Microsoft.AspNetCore.Mvc;
using API.Data;
using API.Entities;
using API.DTOs;
using API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers {
public class AccountController : BaseApiController
{
    private readonly DataContext _context;
    private readonly ITokenService _tokenService;
    
    public AccountController(DataContext context, ITokenService tokenService){
       _context = context;
       _tokenService = tokenService;
    }
    
    [HttpPost("register")] //api/Account/register
    public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto)
    {      
        if (await UserExists(registerDto.Username)) return BadRequest("Username is taken.");
        
        using var hmac =  new System.Security.Cryptography.HMACSHA512();

        var user = new AppUser {
            Username = registerDto.Username.ToLower(),
            PasswordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(registerDto.Password)),
            PasswordSalt = hmac.Key
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return new UserDto 
        {
            Username = user.Username,
            Token = _tokenService.CreateToken(user)
        };
    }

    [HttpPost("login")]
    public async Task<ActionResult<UserDto>> Login(LoginDto loginDto) {
        var user = await _context.Users.SingleOrDefaultAsync(x => x.Username == loginDto.Username);
    
         if (user == null) return Unauthorized();

         using var hmac = new System.Security.Cryptography.HMACSHA512(user.PasswordSalt);

         var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(loginDto.Password));

         for(int i =0; i<computedHash.Length; i++){
            if (computedHash[i] != user.PasswordHash[i]) return Unauthorized("invalid password");

         }

         return new UserDto {
        Username = user.Username,
        Token = _tokenService.CreateToken(user)
      };
    }

    private async Task<bool> UserExists(string username ){
        return await _context.Users.AnyAsync(x => x.Username == username.ToLower());
    }
  }  
}