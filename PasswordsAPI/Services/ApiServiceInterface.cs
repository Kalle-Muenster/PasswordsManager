using System;

namespace PasswordsAPI.Services
{

    public interface IPasswordsApiService 
    {
        Status Status { get; }
        bool  Ok    { get; set; }
    }


    public interface IPasswordsApiService<E>
        : IPasswordsApiService
        where E : class
    {
        IPasswordsApiService srv();
    }


    public interface IPasswordsApiService<E,S>
        : IPasswordsApiService<E>
        where E : EntityBase
        where S : IPasswordsApiService<E>
    {
        S serve();
        S OnError( IPasswordsApiService malfunctioned );
        E Entity { get; set; }
    }


    public abstract class AbstractApiService<E,S>
        : IPasswordsApiService<E,S>
        where E : EntityBase
        where S : AbstractApiService<E,S>
    {
        protected PasswordsDbContext? db;
        protected abstract Status GetDefaultError();
        public abstract E Entity { get; set; }

        private Status status;
        public  Status Status { get => status; 
            protected set => status = value; }

        public virtual bool Ok {
            get { return (status.Code & ErrorCode.IsValid) < ErrorCode.Unknown; }
            set { if ( value ) status = Status.Success;
             else if (status.Code > ErrorCode.Success )
                 status = GetDefaultError();
            }
        }

        // tunneling an status detected by another service then
        // that one where the status was happening, way back to 
        // that service to let generate proper status code there
        // which can point out where that errors was caused.
        public S OnError( IPasswordsApiService otherService )
        {
            if ( otherService.Status.Code.HasFlag( ErrorCode.Service ) ) {
                if( otherService.Status.Data.ToString() == "" ) {
                    status = otherService.Status.WithData( GetType().Name );
                } else {
                    status = otherService.Status.WithText(
                        $"{GetType().Name}: {otherService.Status.Text}"
                    );
                }
            } else {
                status = new Status(
                    ErrorCode.Unknown|ErrorCode.Service|
                    otherService.Status.Code,
                    otherService.Status.Text,
                    this.GetType().Name
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
            status = Status.NoError;
            db = ctx;
        }

        public static implicit operator bool( AbstractApiService<E,S> srv )
        {
            return srv.Ok;
        }
    }
}
