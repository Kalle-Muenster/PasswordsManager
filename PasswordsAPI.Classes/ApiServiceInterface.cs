using System;
using System.Threading.Tasks;
using PasswordsAPI.BaseClasses;
using Microsoft.EntityFrameworkCore;
using Consola;
using PasswordsAPI.Database;

namespace PasswordsAPI.Services
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
        IPasswordsApiService<D> srv();
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
        protected D? _db;

        D IPasswordsApiService<D>.db
        {
            get { return _db; }
        }

        protected abstract Status GetDefaultError();

        protected E          _enty;
        protected Task<E>    _lazy;
        protected StdStreams _cons;

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
        public S OnError( IPasswordsApiService<D> otherService )
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

        public IPasswordsApiService<D> srv()
        {
            return this;
        }

        protected AbstractApiService( D ctx )
        {
            _cons = new StdStreams();
            _enty = new E();
            _enty.Is().Status = status = GetDefaultError();
            _db = ctx;
        }

        public static implicit operator bool( AbstractApiService<E,S,D> srv )
        {
            return srv.Ok;
        }
    }
}
