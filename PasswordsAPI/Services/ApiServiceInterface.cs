using System;

namespace PasswordsAPI.Services
{

    public interface IPasswordsApiService 
    {
        Error Error { get; }
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
        protected abstract Error GetDefaultError();
        public abstract E Entity { get; set; }

        private Error error;
        public  Error Error { get => error; 
            protected set => error = value; }

        public virtual bool Ok {
            get { return error.Code == ErrorCode.NoError; }
            set { if ( value ) error = Error.NoError;
             else if (error.Code == ErrorCode.NoError)
                 error = GetDefaultError();
            }
        }

        // tunneling an error detected by another service then
        // that one where the error was happening, way back to 
        // that service to let generate proper error code there
        // which can point out where that errors was caused.
        public S OnError( IPasswordsApiService otherService )
        {
            if ( otherService.Error.Code.HasFlag( ErrorCode.Service ) ) {
                if( otherService.Error.Data.ToString() == "" ) {
                    error = otherService.Error.WithData( this.GetType().Name );
                } else {
                    error = otherService.Error.WithText(
                        $"{GetType().Name}: {otherService.Error.Text}"
                    );
                }
            } else {
                error = new Error(
                    ErrorCode.Unknown|ErrorCode.Service|
                    otherService.Error.Code,
                    otherService.Error.Text,
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
            error = Error.NoError;
            db = ctx;
        }

        public static implicit operator bool( AbstractApiService<E,S> srv )
        {
            return srv.Ok;
        }
    }
}
