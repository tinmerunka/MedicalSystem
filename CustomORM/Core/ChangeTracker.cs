namespace CustomORM.Core
{
    /// Stanja u kojima entitet može biti.
    public enum EntityState
    {
        Unchanged,  // Dohvaćen iz baze, nije mijenjan
        Added,      // Novi entitet, treba INSERT
        Modified,   // Promijenjen, treba UPDATE
        Deleted     // Označen za brisanje, treba DELETE
    }

    /// Informacije o praćenom entitetu
    public class EntityEntry
    {
        public object Entity { get; set; }
        public EntityState State { get; set; }
        public Type EntityType { get; set; }

        public EntityEntry(object entity, EntityState state)
        {
            Entity = entity;
            State = state;
            EntityType = entity.GetType();
        }
    }

    /// Prati promjene na entitetima.
    /// Kad pozoveš Add(), Update(), Remove() na DbSet-u,
    /// ChangeTracker to zapamti. Kad pozoveš SaveChanges(), on zna što treba INSERT, UPDATE ili DELETE
    public class ChangeTracker
    {
        // Rječnik svih praćenih entiteta
        // Ključ je sam entitet (object), vrijednost je EntityEntry s informacijama
        private readonly Dictionary<object, EntityEntry> _trackedEntities = new();

        /// Označi entitet kao novi (za INSERT)
        public void TrackAdd(object entity)
        {
            if (_trackedEntities.ContainsKey(entity))
            {
                // Ako je već praćen, promijeni stanje na Added
                _trackedEntities[entity].State = EntityState.Added;
            }
            else
            {
                _trackedEntities[entity] = new EntityEntry(entity, EntityState.Added);
            }
        }

        /// Označi entitet kao modificiran (za UPDATE)
        public void TrackModify(object entity)
        {
            if (_trackedEntities.ContainsKey(entity))
            {
                // Ne mijenjaj ako je Added - novi entitet ostaje Added
                if (_trackedEntities[entity].State != EntityState.Added)
                {
                    _trackedEntities[entity].State = EntityState.Modified;
                }
            }
            else
            {
                _trackedEntities[entity] = new EntityEntry(entity, EntityState.Modified);
            }
        }

        /// Označi entitet za brisanje (za DELETE)
        public void TrackDelete(object entity)
        {
            if (_trackedEntities.ContainsKey(entity))
            {
                var currentState = _trackedEntities[entity].State;

                if (currentState == EntityState.Added)
                {
                    // Ako je novi i odmah ga brišemo - samo ga makni iz trackinga
                    _trackedEntities.Remove(entity);
                }
                else
                {
                    _trackedEntities[entity].State = EntityState.Deleted;
                }
            }
            else
            {
                _trackedEntities[entity] = new EntityEntry(entity, EntityState.Deleted);
            }
        }

        /// Počni pratiti entitet kao nepromijenjen (dohvaćen iz baze)
        public void TrackUnchanged(object entity)
        {
            if (!_trackedEntities.ContainsKey(entity))
            {
                _trackedEntities[entity] = new EntityEntry(entity, EntityState.Unchanged);
            }
        }

        /// Dohvati sve entitete koji imaju određeno stanje
        public IEnumerable<EntityEntry> GetEntries(EntityState state)
        {
            return _trackedEntities.Values.Where(e => e.State == state);
        }

        /// Dohvati sve entitete određenog tipa koji imaju određeno stanje
        public IEnumerable<T> GetEntities<T>(EntityState state) where T : class
        {
            return _trackedEntities.Values
                .Where(e => e.State == state && e.EntityType == typeof(T))
                .Select(e => (T)e.Entity);
        }

        /// Dohvati sve praćene entitete
        public IEnumerable<EntityEntry> GetAllEntries()
        {
            return _trackedEntities.Values;
        }

        /// Provjeri ima li promjena koje treba spremiti
        public bool HasChanges()
        {
            return _trackedEntities.Values.Any(e =>
                e.State == EntityState.Added ||
                e.State == EntityState.Modified ||
                e.State == EntityState.Deleted);
        }

        /// Resetiraj stanje svih entiteta na Unchanged
        /// Poziva se nakon uspješnog SaveChanges()
        public void AcceptAllChanges()
        {
            // Makni obrisane entitete
            var deletedKeys = _trackedEntities
                .Where(kvp => kvp.Value.State == EntityState.Deleted)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in deletedKeys)
            {
                _trackedEntities.Remove(key);
            }

            // Ostale postavi na Unchanged
            foreach (var entry in _trackedEntities.Values)
            {
                entry.State = EntityState.Unchanged;
            }
        }

        /// Potpuno očisti tracker
        public void Clear()
        {
            _trackedEntities.Clear();
        }

        /// Dohvati stanje određenog entiteta
        public EntityState? GetState(object entity)
        {
            if (_trackedEntities.TryGetValue(entity, out var entry))
            {
                return entry.State;
            }
            return null;
        }

        /// Broj praćenih entiteta
        public int Count => _trackedEntities.Count;
    }
}