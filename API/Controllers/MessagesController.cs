using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    public class MessagesController: BaseApiController
    {
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _uow;

        public MessagesController(IUnitOfWork uow, IMapper mapper)
        {
            _uow = uow;
            _mapper = mapper;
        }

        [HttpPost]
        public async Task<ActionResult<MessageDto>> CreateMessage(CreateMessageDto createMessageDto)
        {
            var username = User.GetUsername();
            if(username == createMessageDto.RecipientUsername.ToLower())
                return BadRequest("You cannot send messages to yourself");
            
            var sender = await _uow.UserRepository.GetUserByUsernameAsync(username);
            var recepient = await _uow.UserRepository.GetUserByUsernameAsync(createMessageDto.RecipientUsername);

            if(recepient == null) return NotFound();

            var message = new Message
            {
                Sender = sender,
                Recepient = recepient,
                SenderUsername = sender.UserName,
                RecepientUsername = recepient.UserName,
                Content = createMessageDto.Content
            };

            _uow.MessageRepository.AddMessage(message);

            if(await _uow.Complete()) 
                 return Ok(_mapper.Map<MessageDto>(message));

            return BadRequest("Failed to send message");
        }

        [HttpGet]
        public async Task<ActionResult<PagedList<MessageDto>>> GetMessagesFor([FromQuery]MessageParams messageParams)
        {
            messageParams.Username = User.GetUsername();
            var messages = await _uow.MessageRepository.GetMessagesForUser(messageParams);

            Response.AddPaginationHeader(new PaginationHeader(messages.CurrentPage, 
                         messages.PageSize, messages.TotalCount, messages.TotalPages));
            
            return messages;
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteMessages(int id)
        {
            var username = User.GetUsername();
            var message = await _uow.MessageRepository.GetMessage(id);
            if(message.SenderUsername != username && message.RecepientUsername != username)
               return Unauthorized();
            
            if(message.SenderUsername == username) message.SenderDeleted = true;
            if(message.RecepientUsername == username) message.RecipientDeleted = true;

            if(message.SenderDeleted && message.RecipientDeleted)
               _uow.MessageRepository.DeleteMessage(message);
            
            if(await _uow.Complete()) return Ok();
            return BadRequest("Problem deleting the message");
        }
    }
}