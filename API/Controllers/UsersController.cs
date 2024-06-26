using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize]
    public class UsersController: BaseApiController
    {
        private readonly IMapper _map;
        private readonly IPhotoService _photoService;
        private readonly IUnitOfWork _uow;

        public UsersController(IUnitOfWork uow, IMapper map, IPhotoService photoService)
        {
            _uow = uow;
            _map = map;
            _photoService = photoService;
        }
        
        
        [HttpGet("{username}")]
        public async Task<ActionResult<MemberDto>> GetUser(string username)
        {
           return await _uow.UserRepository.GetMemberByUsernameAsync(username);
        }
        
        [HttpGet]
        public async Task<ActionResult<PagedList<MemberDto>>> GetUsers([FromQuery]UserParams userParams)
        {
            var gender = await _uow.UserRepository.GetUserGender(User.GetUsername());
            userParams.CurrentUsername = User.GetUsername();
            if(string.IsNullOrEmpty(userParams.Gender))
            {
                userParams.Gender = gender == "male" ? "female" : "male";
            }
           var users = await _uow.UserRepository.GetMembersAsync(userParams);
           Response.AddPaginationHeader(new PaginationHeader(users.CurrentPage, users.PageSize, users.TotalCount, users.TotalPages));
           return Ok(users);
        }

        [HttpPut]
        public async Task<ActionResult> Update(MemberUpdateDto memberUpdateDto)
        {
            var user = await _uow.UserRepository.GetUserByUsernameAsync(User.GetUsername());

            if(user == null) return NotFound();

            _map.Map(memberUpdateDto, user);

            if(await _uow.Complete()) return NoContent();

            return BadRequest("Failed to update user");
        }

        [HttpPost("add-photo")]
        public async Task<ActionResult<PhotoDto>> AddPhoto(IFormFile file)
        {
            var user = await _uow.UserRepository.GetUserByUsernameAsync(User.GetUsername());

            if(user == null) return NotFound();

            var result = await _photoService.AddPhotoAsync(file);
            if(result.Error != null) return BadRequest(result.Error.Message);

            var photo = new Photo
            {
                Url = result.SecureUrl.AbsoluteUri,
                PublicId = result.PublicId
            };

            if(user.Photos.Count == 0) photo.IsMain = true;

            user.Photos.Add(photo);

            if(await _uow.Complete())
               return _map.Map<PhotoDto>(photo);
               
             return BadRequest("Problem adding photo");
        } 

        [HttpPut("set-main-photo/{photoId}")]
        public async Task<ActionResult> SetMainpPhoto(int photoId)
        {
           var user = await _uow.UserRepository.GetUserByUsernameAsync(User.GetUsername());
           if(user == null) return NotFound();

           var photo = user.Photos.FirstOrDefault(q => q.Id == photoId);
           if(photo == null ) return NotFound();

           if(photo.IsMain) return BadRequest("this is already your main photo");
           var currentMain = user.Photos.FirstOrDefault(q => q.IsMain);

           if(currentMain != null) currentMain.IsMain = false;

           photo.IsMain = true;

           if(await _uow.Complete()) return NoContent();

           return BadRequest("Problem setting the main photo");
        }

        [HttpDelete("delete-photo/{photoId}")]
        public async Task<ActionResult> DeletePhoto(int PhotoId)
        {
            var user = await _uow.UserRepository.GetUserByUsernameAsync(User.GetUsername());

            var photo = user.Photos.FirstOrDefault(q => q.Id == PhotoId);
            if(photo == null) return NotFound();
            if(photo.IsMain) return BadRequest("You cannot delete your mai photo!");

           if(photo.PublicId != null)
           { 
             var result = await _photoService.DeletePhotoAsync(photo.PublicId);
             if(result.Error != null) return BadRequest(result.Error.Message);
           }

           user.Photos.Remove(photo);
           if(await _uow.Complete()) return Ok();
           
           return BadRequest("Problem deleting photo");
        }
    }
}