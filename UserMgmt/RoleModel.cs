using NPoco;
using FluentValidation;
namespace Intellimix_Template.UserMgmt
{
    [TableName("roles")]
    [PrimaryKey("id", AutoIncrement = true)]
    public class RoleModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }= DateTime.Now;
        public DateTime? UpdatedAt { get; set; } = null;
    }



    public class RoleValidator : AbstractValidator<RoleModel>
    {
        public RoleValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Role name should not be empty");
            RuleFor(x => x.Description).MaximumLength(250).WithMessage("Description should not exceed 250 characters");
        }
    }

    [TableName("departments")]
    [PrimaryKey("Id", AutoIncrement = true)]
    public class  Department
    {
        public int Id { get; set; }
        public string Name { get; set; }
      

    }
}
