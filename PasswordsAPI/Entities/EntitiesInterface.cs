namespace PasswordsAPI
{

    public interface IEntityBase
    {
        Error Error { get; set; }
        IEntityBase Is();
    }


    public class EntityBase : IEntityBase
    {
        public static readonly EntityBase Invalid = new EntityBase( new Error( ErrorCode.Invalid ) );

        Error  IEntityBase.Error { get; set; }
        public IEntityBase Is() { return this; }

        public static implicit operator bool( EntityBase cast ) {
            return cast.IsValid();
        }
        public static implicit operator EntityBase( Error wasError ) {
            return new EntityBase( wasError );
        }


        public EntityBase()
        {
            Is().Error = Error.NoError;
        }

        public EntityBase( EntityBase copy )
        {
            Is().Error = copy.Is().Error;
        }

        public EntityBase( Error wasError )
        {
            Is().Error = wasError;
        }


        public bool IsValid()
        {
            return Is().Error.Code == ErrorCode.NoError;
        }

        public override string ToString()
        {
            return Is().Error.ToString();
        }
    }
}
