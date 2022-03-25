namespace PasswordsAPI.Abstracts
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


        public EntityBase()
        {
            Is().Status = Status.NoState;
        }

        public EntityBase( Status state )
        {
            Is().Status = state;
        }


        public static implicit operator bool( EntityBase<E> cast ) {
            return cast.IsValid();
        }
        public static implicit operator EntityBase<E>( Status cast ) {
            return new EntityBase<E>( cast );
        }


        public bool IsValid() 
        {
            return !Is().Status.Bad;
        }

        public bool Waiting()
        {
            return Is().Status.IsWaiting;
        }

        public override string ToString()
        {
            return Is().Status.ToString();
        }
    }
}
