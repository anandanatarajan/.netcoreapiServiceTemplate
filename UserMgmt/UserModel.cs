using FluentValidation;
using Intellimix_Template.utils;
using NPoco;
using Riok.Mapperly.Abstractions;

namespace Intellimix_Template.UserMgmt
{
    [TableName("users")]
    [PrimaryKey("id", AutoIncrement = true)]
    public class UserModel
    {
        public int id { get; set; }
        public string name { get; set; }
        public string? email { get; set; }
        public string password { get; set; }
        public int? roleid { get; set; }
        public int? departmentid { get; set; }
    }


    public class UserValidator : AbstractValidator<UserModel>
    {
        public UserValidator()
        {
            RuleFor(x => x.email).NotEmpty().Must(UtilityFunctions.CheckEmailid).WithMessage("Invalid Email");
            RuleFor(x => x.password).NotNull().NotEmpty().Must(UtilityFunctions.ValidatePassword).WithMessage("Invalid password");
            RuleFor(x => x.name).NotNull().NotEmpty().WithMessage("Name should not be empty ");
        }



    }


    public class UserUpdateModel
    {
        public int id { get; set; }
        public string name { get; set; }
        public string? email { get; set; }
        public string? password { get; set; }
        public int? roleId { get; set; }
        public int? departmentId { get; set; }
    }

    [Mapper]
    public partial class UserMapper
    {
        [MapProperty(nameof(UserModel.id), nameof(UserUpdateModel.id))]
        public partial UserUpdateModel ToUpdateModel(UserModel user);
        public partial UserModel FromUpdateModel(UserModel user);

    }


    public class UserModelService
    {
        //  private readonly SimpleEncryption _simpleEncryption;
        private readonly ILogger<UserModelService> _logger;
        public UserModelService(ILogger<UserModelService> logger)
        {

            _logger = logger;
        }
        public UserModel DecryptPassword(UserModel user)
        {
            user.password = SimpleEncryption.Decrypt(user.password);
            return user;
        }
        public UserModel EncryptPassword(UserModel user)
        {
            user.password = SimpleEncryption.Decrypt(user.password);
            return user;
        }

        public bool IsAuthenticated(UserModel user, string plainTextPassword)
        {
            string decryptedPassword = "";
            decryptedPassword = SimpleEncryption.Decrypt(user.password);
            return decryptedPassword == plainTextPassword;
        }

    }

    public class UserFilter
    {
        public string? name { get; set; }
        public string? email { get; set; }
        public int? roleId { get; set; }
        public int? departmentId { get; set; }
    }



}
