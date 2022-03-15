namespace PasswordsAPI
{

    public interface IEntityBase
    {
        Status Status { get; set; }
        IEntityBase Is();
        E Entity<E>() where E : IEntityBase;
    }


    public class EntityBase<E> : IEntityBase where E : EntityBase<E>, new()
    {
        public static readonly EntityBase<E> Invalid = new( new Status( ResultCode.Invalid ) );

        Status IEntityBase.Status { get; set; }
        public IEntityBase Is() { return this; }
        ET IEntityBase.Entity<ET>() { return (ET)Is(); }
        public E Entity() { return Is().Entity<E>(); }
        


        public static implicit operator bool( EntityBase<E> cast ) {
            return cast.IsValid();
        }
        public static implicit operator EntityBase<E>( Status wasError ) {
            return new EntityBase<E>( wasError );
        }


        public EntityBase()
        {
            Is().Status = Status.NoError;
        }

        public EntityBase( Status wasError )
        {
            Is().Status = wasError;
        }

        public bool IsValid()
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
