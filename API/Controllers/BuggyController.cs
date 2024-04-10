using API.Data;
using API.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    public class BuggyController : BaseApiController
    {
        private readonly AppDbContext _db;

        public BuggyController(AppDbContext db)
        {
            _db = db;
        }
        
        [Authorize]
        [HttpGet("auth")]
        public ActionResult<string> GetSecret()
        {
            return "secret text";
        }
        [HttpGet("not-found")]
        public ActionResult<AppUser> GetNotFound()
        {
            var thing = _db.Users.Find(-1);
            if(thing == null) return NotFound();
            return thing;
        }
        [HttpGet("server-error")]
        public ActionResult<string> GetServerEerror()
        {
            var thing = _db.Users.Find(-1);
            var thingToString = thing.ToString();
            return thingToString;
        }
        [HttpGet("bad-request")]
        public ActionResult<string> GetBadRequest()
        {
            return BadRequest("This is not a good request");
        }
    }
}