using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace PasswordsAPI
{

    public class PasswordUsers : EntityBase
    {
        public new static readonly PasswordUsers Invalid = new PasswordUsers(
            new Error( ErrorCode.Invalid|ErrorCode.User|ErrorCode.Data ) );

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

        public PasswordUsers( Error code )
            : base( code ) {
            Is().Error = code.Code > 0 
                  ? code : Invalid.Is().Error;
            Info = String.Empty;
            Name = String.Empty;
            Mail = String.Empty;
            Id = -1;
        }

        public static implicit operator PasswordUsers( Error cast ) {
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
        }

        //public override void SetTo<T>( T assignee )
        //{
        //    base.SetTo( assignee );
        //    if ( typeof( T ) == GetType() )
        //    {
        //        if ( ( assignee as PasswordUsers ) != null )
        //        {
        //            PasswordUsers copy = assignee as PasswordUsers;
        //            Id = copy.Id;
        //            Name = copy.Name;
        //            Mail = copy.Mail;
        //            Info = copy.Info ?? String.Empty;
        //        }
        //    }
        //}
    }
}
