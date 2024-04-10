using API.DTOs;
using API.Entities;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public class MessageRepository : IMessageRepository
    {
        private readonly AppDbContext _db;
        private readonly IMapper _mapper;

        public MessageRepository(AppDbContext db, IMapper mapper)
        {
            _mapper = mapper;
            _db = db;
        }

        public void AddGroup(Group group)
        {
            _db.Groups.Add(group);
        }

        public void AddMessage(Message message)
        {
            _db.Messages.Add(message);
        }

        public void DeleteMessage(Message message)
        {
            _db.Messages.Remove(message);
        }

        public async Task<Connection> GetConnection(string connectionId)
        {
            return await _db.Connections.FindAsync(connectionId);
        }

        public async Task<Group> GetGroupForConnection(string connectionId)
        {
            return await _db.Groups
                  .Include(q => q.Connections)
                  .Where(q => q.Connections.Any(c => c.ConnectionId == connectionId))
                  .FirstOrDefaultAsync();
        }

        public async Task<Message> GetMessage(int id)
        {
            return await _db.Messages.FindAsync(id);
        }

        public async Task<Group> GetMessageGroup(string groupName)
        {
            return await _db.Groups.Include(q => q.Connections)
                         .FirstOrDefaultAsync(q => q.Name == groupName);
        }

        public async Task<PagedList<MessageDto>> GetMessagesForUser(MessageParams messageParams)
        {
            var query = _db.Messages
                .OrderByDescending(q => q.MessageSent)
                .AsQueryable();
            
            query = messageParams.Container switch
            {
                "Inbox" => query.Where(q => q.RecepientUsername == messageParams.Username && q.RecipientDeleted == false),
                "Outbox" => query.Where(q => q.SenderUsername == messageParams.Username && q.SenderDeleted == false),
                _ => query.Where(q => q.RecepientUsername == messageParams.Username 
                                && q.RecipientDeleted == false && q.DateRead == null)
            };

            var messages = query.ProjectTo<MessageDto>(_mapper.ConfigurationProvider);
            return await PagedList<MessageDto>
                 .CreateAsync(messages, messageParams.PageNumber, messageParams.PageSize);
        }

        public async Task<IEnumerable<MessageDto>> GetMessageThread(string currentUserName, string recipientUserName)
        {
            var query = _db.Messages
                .Where(
                    q => q.RecepientUsername == currentUserName && q.RecipientDeleted == false &&
                    q.SenderUsername == recipientUserName ||
                    q.RecepientUsername == recipientUserName && q.SenderDeleted == false &&
                    q.SenderUsername == currentUserName
                )
                .OrderBy(q => q.MessageSent)
                .AsQueryable();

            var unreadMessages = query.Where(q => q.DateRead == null && 
                            q.RecepientUsername == currentUserName).ToList();
            
            if(unreadMessages.Any())
            {
                foreach (var message in unreadMessages)
                {
                    message.DateRead = DateTime.UtcNow;
                }
            }

            return await query.ProjectTo<MessageDto>(_mapper.ConfigurationProvider).ToListAsync();
        }

        public void RemoveConection(Connection connection)
        {
            _db.Connections.Remove(connection);
        }
    }
}