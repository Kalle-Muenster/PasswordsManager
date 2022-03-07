using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Yps;

namespace PasswordsAPI
{

    public class UserPasswords : EntityBase
    {
        public new static readonly UserPasswords Invalid = 
            new UserPasswords(new Error(ErrorCode.Invalid|ErrorCode.Word));

        [Key]
        public int Id { get; set; }
        [Required]
        [ForeignKey("PasswordUsers")]
        public int User { get; set; }
        [Required]
        public ulong Hash { get; set; }
        [MaybeNull]
        public string Pass { get; set; }

        //-----------------------------//

        public Crypt.Key GetUserKey() {
            return Crypt.CreateKey( Hash );
        }

        public UserPasswords( Error invalid )
            : base( invalid ) {
            Is().Error = invalid.Code > 0 
                  ? invalid : Invalid.Is().Error;
        }

        public UserPasswords() : base()
        {
            Hash = 0;
            Pass = String.Empty;
        }

        public static implicit operator UserPasswords( Error cast ) {
            return new UserPasswords( cast );
        }

        public UserPasswords( UserPasswords copy ) : base( copy )
        {
            Id = copy.Id;
            User = copy.User;
            Hash = copy.Hash; 
            Pass = copy.Pass ?? String.Empty;
        }

        //public override void SetTo<T>( T assignee )
        //{
        //    base.SetTo( assignee );
        //    if ( typeof( T ) == GetType() )
        //    {
        //        UserPasswords copy = assignee as UserPasswords;
        //        Id = copy.Id;
        //        User = copy.User;
        //        Hash = copy.Hash;
        //        Pass = copy.Pass ?? String.Empty;
        //    }
        //}
    }
}
