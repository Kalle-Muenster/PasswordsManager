using System.Collections;
using Microsoft.EntityFrameworkCore;


namespace PasswordsAPI.Abstracts
{
    public interface IPasswordaApiDbContext<CTX> where CTX : DbContext
    {
        DbSet<MOD> EntitiesSet<MOD>() where MOD : EntityBase<MOD>, new();
        void SetEntities<MOD>(DbSet<MOD> dbSet ) where MOD : EntityBase<MOD>, new();
    }

    public abstract class PasswordsApiDbContext<CTX>
        : DbContext
        , IPasswordaApiDbContext<CTX>
        where CTX : PasswordsApiDbContext<CTX>
    {
        private Hashtable _dbSets = new Hashtable();

        public PasswordsApiDbContext( DbContextOptions<CTX> options ) : base (options)
        {}

        public DbSet<MOD> EntitiesSet<MOD>()
            where MOD : EntityBase<MOD>, new()
        {
            return _dbSets[typeof(MOD)] as DbSet<MOD>;
        }

        public void SetEntities<MOD>( DbSet<MOD> addDbSet )
            where MOD : EntityBase<MOD>, new()
        {
            if ( addDbSet != null )
                _dbSets[typeof(MOD)] = addDbSet;
        }
    }
}
