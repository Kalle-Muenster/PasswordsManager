namespace PasswordsAPI
{

    public interface IEntityBase
    {
        Status Status { get; set; }
        IEntityBase Is();
    }


    public class EntityBase : IEntityBase
    {
        public static readonly EntityBase Invalid = new EntityBase( new Status( ErrorCode.Invalid ) );

        Status IEntityBase.Status { get; set; }
        public IEntityBase Is() { return this; }

        public static implicit operator bool( EntityBase cast ) {
            return cast.NoError();
        }
        public static implicit operator EntityBase( Status wasError ) {
            return new EntityBase( wasError );
        }


        public EntityBase()
        {
            Is().Status = Status.NoError;
        }

        public EntityBase( EntityBase copy )
        {
            Is().Status = copy.Is().Status;
        }

        public EntityBase( Status wasError )
        {
            Is().Status = wasError;
        }


        public bool NoError()
        {
            return ( Is().Status.Code & ErrorCode.IsValid ) < ErrorCode.Unknown;
        }

        public bool HasInfo()
        {
            return ( Is().Status.Code & ErrorCode.IsValid ) == ErrorCode.Success;
        }

        public override string ToString()
        {
            return Is().Status.ToString();
        }
    }
}
