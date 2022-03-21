using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using PasswordsAPI.BaseClasses;

namespace PasswordsAPI.Models
{
    
    public class UserLocations : EntityBase<UserLocations>
    {
        public new static readonly UserLocations Invalid =
            new ( new Status( 
                ResultCode.User | ResultCode.Area |
                ResultCode.Invalid, "Location not valid" )
            );
        
        /*-----------------------------*/

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

        public UserLocations()
            : base()
        {
            Area = Info = Name = String.Empty;
            Pass = Array.Empty<byte>();
            Id = 0;
        }

        public UserLocations( Status invalid )
            : base( invalid )
        {
            Area = String.Empty;
            Pass = Array.Empty<byte>();
            Is().Status = invalid.Code == ResultCode.NoError
                  ? Invalid.Is().Status : invalid;
            Id = -1;
        }

        public static implicit operator UserLocations( Status cast ) {
            return new UserLocations( cast );
        }
    }
}
