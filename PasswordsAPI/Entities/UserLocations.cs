using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace PasswordsAPI
{
    
    public class UserLocations : EntityBase
    {
        public new static readonly UserLocations Invalid =
            new UserLocations( new Error( 
                ErrorCode.User | ErrorCode.Area |
                ErrorCode.Invalid, "Location not valid" )
            );

        [Key]
        public int      Id { get; set; }
        [Required]
        public string Area { get; set; }
        [MaybeNull]
        public string Info { get; set; }
        [Required]
        [ForeignKey("PasswordUsers")]
        public int    User { get; set; }
        [MaybeNull]
        public string Name { get; set; }
        [Required]
        public byte[] Pass { get; set; }

        /*-----------------------------*/

        public UserLocations() : base()
        {
            Area = Info = Name = String.Empty;
            Pass = Array.Empty<byte>();
        }

        public UserLocations( Error invalid )
            : base(invalid)
        {
            Area = String.Empty;
            Pass = Array.Empty<byte>();
            Is().Error = invalid.Code == ErrorCode.NoError
                  ? Invalid.Is().Error : invalid;
        }

        public static implicit operator UserLocations( Error cast )
        {
            return new UserLocations( cast );
        }

        public UserLocations(UserLocations copy) : base(copy)
        {
            Id = copy.Id;
            Area = copy.Area;
            Info = copy.Info ?? String.Empty;
            User = copy.User;
            Name = copy.Name ?? String.Empty;
            Pass = copy.Pass;
        }
    }
}
