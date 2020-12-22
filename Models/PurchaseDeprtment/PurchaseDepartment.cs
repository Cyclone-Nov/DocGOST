using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GostDOC.Context;
using GostDOC.Events;
using GostDOC.Common;
using Microsoft.EntityFrameworkCore;

namespace GostDOC.Models
{
    class PurchaseDepartment : IDisposable
    {
        private DatabaseContext _db;

        private static NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger();

        #region Singleton
        private static readonly Lazy<PurchaseDepartment> _instance = new Lazy<PurchaseDepartment>(() => new PurchaseDepartment(), true);
        public static PurchaseDepartment Instance => _instance.Value;        
        #endregion

        public event EventHandler<TEventArgs<ICollection<Supplier>>> SupplierFromDBEvent;
        public event EventHandler<TEventArgs<ICollection<WarehouseAcceptance>>> WarehouseAcceptanceFromDBEvent;
        public event EventHandler<TEventArgs<ICollection<WarehouseDelivery>>> WarehouseDeliveryFromDBEvent;

        public event EventHandler<TEventArgs<ComponentSupplierProfile>> ComponentSupplierProfileFromDBEvent;

        public string DBFileName => _db?.FileName;
        public string ComponentName { get; set; }

        public PurchaseDepartment()
        {
        }

        public bool ConnectToDB(string aFileName)
        {
            try
            {
                _db = new DatabaseContext(aFileName);
                return true;
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }
            return false;
        }

        public bool SetWarehouseAcceptanceToDB(ICollection<WarehouseAcceptance> aAcceptances)
        {
            try
            {
                var profile = _db.Profiles.Include(x => x.WarehouseAcceptances).First(x => x.ComponentName == ComponentName);
                if (profile != null)
                {
                    profile.WarehouseAcceptances.Clear();
                    profile.WarehouseAcceptances.AddRange(aAcceptances);
                    _db.SaveChanges();
                    return true;
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }
            return false;
        }


        public bool SetWarehouseDeliveryToDB(ICollection<WarehouseDelivery> aDeliveries)
        {
            try
            {
                var profile = _db.Profiles.Include(x => x.WarehouseDeliveries).First(x => x.ComponentName == ComponentName);
                if (profile != null)
                {
                    profile.WarehouseDeliveries.Clear();
                    profile.WarehouseDeliveries.AddRange(aDeliveries);
                    _db.SaveChanges();
                    return true;
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }
            return false;
        }


        public bool SetSuppliersToDB(ICollection<Supplier> aSuppliers)
        {
            try
            {
                var profile = _db.Profiles.Include(x => x.Suppliers).First(x => x.ComponentName == ComponentName);
                if (profile != null)
                {
                    profile.Suppliers.Clear();
                    profile.Suppliers.AddRange(aSuppliers);
                    _db.SaveChanges();
                    return true;
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }
            return false;
        }

        public bool SetComponentSupplierProfileToDB(ComponentSupplierProfile aProfile)
        {
            try
            {
                var profile = _db.Profiles.FirstOrDefault(x => x.ComponentName == aProfile.ComponentName);
                if (profile == null)
                {
                    _db.Profiles.Add(aProfile);
                    _db.SaveChanges();
                    return true;
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }
            return false;
        }

        public bool GetComponentSupplierProfile()
        {
            try
            {
                var profile = _db.Profiles
                    .Include(x => x.Properties)
                    .Include(x => x.Suppliers)
                    .Include(x => x.WarehouseAcceptances)
                    .Include(x => x.WarehouseDeliveries)
                    .Include(x => x.ComponentsEntry)
                    .FirstOrDefault(x => x.ComponentName == ComponentName);

                if (profile != null)
                {
                    ComponentSupplierProfileFromDBEvent?.Invoke(this, new TEventArgs<ComponentSupplierProfile>(profile));
                    return true;
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }
            return false;
        }

        public bool GetComponentWarehouseDelivery()
        {
            try
            {
                var profile = _db.Profiles.Include(x => x.WarehouseDeliveries).FirstOrDefault(x => x.ComponentName == ComponentName);
                if (profile != null)
                {
                    WarehouseDeliveryFromDBEvent?.Invoke(this, new TEventArgs<ICollection<WarehouseDelivery>>(profile.WarehouseDeliveries));
                    return true;
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }
            return false;
        }

        public bool GetComponentWarehouseAcceptance()
        {
            try
            {
                var profile = _db.Profiles.Include(x => x.WarehouseAcceptances).FirstOrDefault(x => x.ComponentName == ComponentName);
                if (profile != null)
                {
                    WarehouseAcceptanceFromDBEvent?.Invoke(this, new TEventArgs<ICollection<WarehouseAcceptance>>(profile.WarehouseAcceptances));
                    return true;
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }
            return false;
        }

        public bool GetSuppliers()
        {
            try
            {
                var profile = _db.Profiles.Include(x => x.Suppliers).FirstOrDefault(x => x.ComponentName == ComponentName);
                if (profile != null)
                {
                    SupplierFromDBEvent?.Invoke(this, new TEventArgs<ICollection<Supplier>>(profile.Suppliers));
                    return true;
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }
            return false;
        }

        #region IDisposable Interface Implementation

        private volatile bool _isDisposed = false;

        /// <summary>
        /// Dispose file stream
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose file stream
        /// </summary>
        protected virtual void Dispose(bool isDisposing)
        {
            if (_isDisposed)
            {
                return;
            }

            if (isDisposing)
            {
                _db?.Dispose();
            }

            _isDisposed = true;
        }

        #endregion IDisposable Interface Implementation
    }
}
