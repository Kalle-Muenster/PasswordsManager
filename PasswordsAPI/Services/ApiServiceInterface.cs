using System;
using System.Threading.Tasks;

namespace PasswordsAPI.Services
{

    public interface IPasswordsApiService 
    {
        Status Status { get; }
        bool  Ok    { get; }
    }


    public interface IPasswordsApiService<E>
        : IPasswordsApiService
        where E : IEntityBase, new()
    {
        IPasswordsApiService srv();
    }


    public interface IPasswordsApiService<E,S>
        : IPasswordsApiService<E>
        where E : EntityBase<E>, new()
        where S : IPasswordsApiService<E>
    {
        S serve();
        S OnError( IPasswordsApiService malfunctioned );
        E Entity { get; set; }
    }


    public abstract class AbstractApiService<E,S>
        : IPasswordsApiService<E,S>
        where E : EntityBase<E>, new()
        where S : AbstractApiService<E,S>
    {
        protected PasswordsDbContext? _db;
        protected abstract Status GetDefaultError();

        protected E       _enty;
        protected Task<E> _lazy;

        public abstract E Entity { get; set; }

        private Status status;
        public  Status Status { get => status; 
            protected set => status = value; }

        public virtual bool Ok {
            get { return (status.Code & ResultCode.IsValid) < ResultCode.Unknown; }
            protected set { if ( value ) status = Status.Success;
             else if (status.Code > ResultCode.Success )
                 status = GetDefaultError();
            }
        }

        // tunneling an status detected by another service then
        // that one where the status was happening, way back to 
        // that service to let generate proper status code there
        // which can point out where that errors was caused.
        public S OnError( IPasswordsApiService otherService )
        {
            if ( otherService.Status.Code.HasFlag( ResultCode.Service ) ) {
                if( otherService.Status.Data.ToString() == "" ) {
                    status = otherService.Status.WithData( GetType().Name );
                } else {
                    status = otherService.Status.WithText(
                        $"{GetType().Name}: {otherService.Status.Text}"
                    );
                }
            } else {
                status = new Status(
                    ResultCode.Unknown|ResultCode.Service|
                    otherService.Status.Code,
                    otherService.Status.Text,
                    GetType().Name
                );
            } return this.serve();
        }

        public S serve()
        {
            return (S)this;
        }

        public IPasswordsApiService srv()
        {
            return this;
        }

        protected AbstractApiService( PasswordsDbContext ctx )
        {
            _enty = new E();
            _enty.Is().Status = status = GetDefaultError();
            _db = ctx;
        }

        public static implicit operator bool( AbstractApiService<E,S> srv )
        {
            return srv.Ok;
        }
    }
}
