namespace Passwords.API.Abstracts
{

    public interface IEntityBase
    {
        Status DefaultStatus { get; }
        Status Status { get; set; }
        IEntityBase Is();
        E Entity<E>() where E : IEntityBase;
    }


    public class EntityBase<E> 
        : IEntityBase
    where E
        : EntityBase<E>
        , new()
    {
        public static readonly EntityBase<E> Invalid = new( new Status( ResultCode.Invalid ) );

        Status IEntityBase.DefaultStatus { get { return new Status(ResultCode.Empty); } }
        Status IEntityBase.Status { get; set; }
        public IEntityBase Is() { return this; }
        ET IEntityBase.Entity<ET>() { return (ET)Is(); }
        public E Entity() { return Is().Entity<E>(); }


        public EntityBase()
        {
            Is().Status = Is().DefaultStatus;
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
            return Is().Status.Intermediate;
        }

        public override string ToString()
        {
            return Is().Status.ToString();
        }
    }
}
