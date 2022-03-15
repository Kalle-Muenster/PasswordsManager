using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace PasswordsAPI
{

    public class PasswordUsers : EntityBase
    {
        public new static readonly PasswordUsers Invalid = new PasswordUsers(
            new Status( ResultCode.Invalid|ResultCode.User|ResultCode.Data ) );

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
            Is().Status = code.Code > 0 
                  ? code : Invalid.Is().Status;
            Info = String.Empty;
            Name = String.Empty;
            Mail = String.Empty;
            Icon = Array.Empty<byte>();
            Id = 0;
        }

        public static implicit operator PasswordUsers( Status cast ) {
            return new PasswordUsers( cast );
        }

        public override string ToString() {
            return string.Format("{{0}}",
                $"\"Id\":{Id},\"Name\":\"{Name}\"" );
        }

        public PasswordUsers( PasswordUsers copy )
            : base( copy )
        {
            Id   = copy.Id;
            Name = copy.Name;
            Mail = copy.Mail;
            Info = copy.Info ?? String.Empty;
            Icon = copy.Icon;
        }
    }
}
