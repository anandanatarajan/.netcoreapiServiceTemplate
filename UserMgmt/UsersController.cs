using Asp.Versioning;
using EasyNetQ;
using Intellimix_Template.Messaging;
using Intellimix_Template.utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Newtonsoft.Json;
using NPoco;
using SupermarketRepository;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Intellimix_Template.UserMgmt
{
    [EnableRateLimiting("fixed")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [Authorize]
    [ApiVersion("1.0")]
    public class UsersController : ControllerBase
    {
        private readonly ILogger<UsersController> _logger;
        private readonly IDBCrud dBClass;
        private readonly Intellimix_Template.Messaging.IEventBus _eventBus;

        public UsersController(ILogger<UsersController> logger, IDBCrud dbc, Intellimix_Template.Messaging.IEventBus eventBus)
        {
            _logger = logger;
            dBClass = dbc;
            dBClass.GetCommand += DBClass_GetCommand;
            dBClass.MailSendingRequested += DBClass_MailSendingRequested; 
            _eventBus = eventBus;
        }

        private void DBClass_MailSendingRequested(object? sender, bool e)
        {
            if (e)
            {
                _logger.LogInformation("Mail sending requested by " + nameof(sender));

            }
        }

        private void DBClass_GetCommand(object? sender, DbCommandEventArgs e)
        {
           _logger.LogInformation(e.CommandText + " executed @ " + e.EDateTime + " requested by " + nameof(sender));
        }


        //GET: api/<UsersController>
        [HttpGet("GetAsync")]
        public async Task<List<UserModel>> GetAsync(CancellationToken token)
        {

            return await dBClass.SelectAllAsync<UserModel>(token);
        }

        [HttpGet]
        public string Get()
        {
            return JsonConvert.SerializeObject(dBClass.SelectAll<UserModel>());
        }

        // GET api/<UsersController>/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
           return JsonConvert.SerializeObject(dBClass.SelectById<UserModel>(id));
        }

        // POST api/<UsersController>
        [HttpPost]
        public IActionResult Post([FromBody] UserModel value)
        {
            var validator = new UserValidator();
            var results = validator.Validate(value);
            if (results.IsValid)
            {
                value.password= SimpleEncryption.Encrypt(value.password);
                dBClass.AddNew(value);
                _eventBus.PublishAsync(value);
                return Ok("User added successfully");
            }
            else
            {
                var errmsg = "";
                foreach (var failure in results.Errors)
                {
                    errmsg += "Property " + failure.PropertyName + " failed validation. Error was: " + failure.ErrorMessage + "\n";
                    _logger.LogError(errmsg??"some error occured");
                }
                return Problem(errmsg);
            }
        }

        // PUT api/<UsersController>/5
        [HttpPut("{id}")]
        public IActionResult Put(int id, [FromBody] UserModel value)
        {
            var validator = new UserValidator();
            var results = validator.Validate(value);
            if (results.IsValid)
            {
               var model= dBClass.SelectById<UserModel>(id);
                if (model is not null) { 
                    UserMapper mapper = new UserMapper();
                    var updateModel = mapper.FromUpdateModel( value);
                   var ret= dBClass.Update(updateModel);
                     if (ret>0)
                          return Ok("User updated successfully");
                     else
                          return Problem("User updation failed");

                }
                else
                {
                    return Problem("User not found");
                }

            }
            else
            {
                var errmsg = "";
                foreach (var failure in results.Errors)
                {
                    errmsg += "Property " + failure.PropertyName + " failed validation. Error was: " + failure.ErrorMessage + "\n";
                    _logger.LogError(errmsg);
                }
                return Problem(errmsg);
            }


        }

        // DELETE api/<UsersController>/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {

            try
            {
                dBClass.Delete<UserModel>(id);
                return Ok("User deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "User deletion failed ");
                return Problem("User deletion failed");
            }
        }

        [HttpGet("Getusers")]
        public IActionResult GetUsers([FromQuery] UserFilter filters, CancellationToken token)
        {   
            
            var sqlbuilder= new SqlBuilder();
            if (filters == null)
                return Problem("Invalid filters");
            if (filters.roleId.HasValue)
            {
                sqlbuilder.Where("roleId=@0", filters.roleId.Value);
            }
            if (filters.departmentId.HasValue)
            {
                sqlbuilder.Where("departmentId=@0", filters.departmentId.Value);
            }
            if (!string.IsNullOrEmpty(filters.name))
            {
                sqlbuilder.Where("name LIKE @0", "%" + filters.name + "%");
            }
            if (!string.IsNullOrEmpty(filters.email))
            {
                sqlbuilder.Where("email LIKE @0", "%" + filters.email + "%");
            }
            sqlbuilder.OrderBy("name ASC");
            var template= sqlbuilder.AddTemplate("SELECT * FROM users WHERE /**where**/ /**orderby**/");
            var userList= dBClass.SelectBySQL<UserModel>(template.RawSql,template.Parameters).ToList();

            return Ok(userList);
        }
    }
}
