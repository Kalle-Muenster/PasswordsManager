using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Consola;

namespace PasswordsAPI.Abstracts
{

    public interface IPasswordsApiService<D> 
        where D : DbContext, IPasswordaApiDbContext<D>
    {
        Status Status { get; }
        bool  Ok    { get; }
        D db { get; }
    }
    

    public interface IPasswordsApiService<E,D>
        : IPasswordsApiService<D>
        where E : IEntityBase, new()
        where D : DbContext, IPasswordaApiDbContext<D>
    {
    }
    

    public interface IPasswordsApiService<E,S,D>
        : IPasswordsApiService<E,D>
        where E : EntityBase<E>, new()
        where S : IPasswordsApiService<E,D>
        where D : DbContext, IPasswordaApiDbContext<D>
    {
        S serve();
        S OnError( IPasswordsApiService<D> malfunctioned );
        E Entity { get; set; }
    }


    public abstract class AbstractApiService<E,S,D>
        : IPasswordsApiService<E,S,D>
        where E : EntityBase<E>, new()
        where S : AbstractApiService<E,S,D>
        where D : DbContext, IPasswordaApiDbContext<D>
    {
        protected D?         _db;
        protected E          _enty;
        protected Task<E>    _lazy;
        protected DbSet<E>   _dset;
        protected StdStreams _cons;
        
        private Status       state;

        protected abstract Status GetDefaultError();

        D IPasswordsApiService<D>.db => _db;
        public S serve() { return (S)this; }
        public abstract E Entity { get; set; }



        protected AbstractApiService( D ctx )
        {
            _db = ctx;
            _enty = new E();
            _cons = new StdStreams();
            state = GetDefaultError();
            _enty.Is().Status = state;
            _dset = ctx.EntitiesSet<E>();
        }


        public Status Status {
            get => state; 
            protected set => state = value;
        }

        public virtual bool Ok {
            get { return ( state.Code & ResultCode.IsValid ) < ResultCode.Unknown; }
            protected set { if ( value ) state = Status.Success;
             else if ( state.Code > ResultCode.Success )
                 state = GetDefaultError();
            }
        }

        public static implicit operator bool( AbstractApiService<E, S, D> cast ) {
            return cast.Ok;
        }

        // tunneling an status detected by another service then
        // that one where the status was happening, way back to 
        // that service to let generate proper status code there
        // which can point out where that errors was caused.
        public S OnError( IPasswordsApiService<D> otherService )
        {
            if ( otherService.Status.Code.HasFlag( ResultCode.Service ) ) {
                if( otherService.Status.Data.ToString() == "" ) {
                    state = otherService.Status.WithData( GetType().Name );
                } else {
                    state = otherService.Status.WithText(
                        $"{GetType().Name}: {otherService.Status.Text}"
                    );
                }
            } else {
                state = new Status(
                    ResultCode.Unknown|ResultCode.Service|
                    otherService.Status.Code,
                    otherService.Status.Text,
                    GetType().Name
                );
            } return this.serve();
        }

    }
}
