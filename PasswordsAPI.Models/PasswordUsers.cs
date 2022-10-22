using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Passwords.API.Abstracts;

namespace Passwords.API.Models
{
    public class PasswordUsers : EntityBase<PasswordUsers>, IEntityBase
    {
        public new static readonly PasswordUsers Invalid = new PasswordUsers(
            new Status( ResultCode.Invalid|ResultCode.User|ResultCode.Data ) );

        Status IEntityBase.DefaultStatus { get { return new Status(ResultCode.User); } }

        [Key]
        public int      Id { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public string Mail { get; set; }
        [MaybeNull]
        public string Info { get; set; }
        [MaybeNull]
        public byte[] Icon { get; set; }

        public PasswordUsers() : base() {
            Name = Mail = Info = String.Empty;
            Icon = Array.Empty<byte>();
        }

        /*----------------------------*/

        public PasswordUsers( Status code )
            : base( code ) {
            Is().Status = code.Code > ResultCode.Success 
                  ? code : Invalid.Is().Status;
            Info = String.Empty;
            Name = String.Empty;
            Mail = String.Empty;
            Icon = Array.Empty<byte>();
            Id = 0;
        }

        public PasswordUsers( string name, string mail, string info )
            : base()
        {
            Info = info;
            Name = name;
            Mail = mail;
            Icon = Array.Empty<byte>();
            Id = 0;
        }

        public static implicit operator PasswordUsers( Status cast )
        {
            return new PasswordUsers( cast );
        }
        
        public static implicit operator bool( PasswordUsers cast )
        {
            return cast.Is().Status;
        }

        public override string ToString() {
            return "{"+ $"\"Id\":{Id},\"Name\":\"{Name}\"" + "}";
        }
    }
}
