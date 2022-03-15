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
            return ( Is().Status.Code & ResultCode.IsValid ) < ResultCode.Unknown;
        }

        public bool HasInfo()
        {
            return ( Is().Status.Code & ResultCode.IsValid ) == ResultCode.Success;
        }

        public bool Waiting()
        {
            return( Is().Status.Code & ResultCode.IsValid ) == ResultCode.Unknown;
        }
        public override string ToString()
        {
            return Is().Status.ToString();
        }
    }
}
