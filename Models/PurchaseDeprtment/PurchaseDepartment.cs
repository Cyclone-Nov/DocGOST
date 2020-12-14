using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GostDOC.Context;
using GostDOC.Events;
using GostDOC.Common;

namespace GostDOC.Models
{
    class PurchaseDepartment // : ?IDisposable? if init DB
    {

        #region Singleton
        private static readonly Lazy<PurchaseDepartment> _instance = new Lazy<PurchaseDepartment>(() => new PurchaseDepartment(), true);
        public static PurchaseDepartment Instance => _instance.Value;        
        #endregion

        public event EventHandler<TEventArgs<ICollection<Supplier>>> SupplierFromDBEvent;
        public event EventHandler<TEventArgs<ICollection<WarehouseAcceptance>>> WarehouseAcceptanceFromDBEvent;
        public event EventHandler<TEventArgs<ICollection<WarehouseDelivery>>> WarehouseDeliveryFromDBEvent;

        public event EventHandler<TEventArgs<ComponentSupplierProfile>> ComponentSupplierProfileFromDBEvent;

        public string DBFileName { get; private set; }
        public string ComponentName { get; private set; }
        public string ConfigurationName { get; private set; }

        public PurchaseDepartment()
        {

            // TODO: init work with DB adapter
        }

        public bool ConnectToDB(string aFileName)
        {
            DBFileName = aFileName;

            // TODO: ?async? connect to DB 

            return false;
        }

        public void ChangeThisComponent(string aConfigurationName, string aComponentName)
        {
            ConfigurationName = aConfigurationName;
            ComponentName = aComponentName;
        }

        public bool SetWarehouseAcceptanceToDB(ICollection<WarehouseAcceptance> aAcceptances)
        {
            // TODO: ?async? set WarehouseAcceptance collection with only changed items to DB
            return false;
        }


        public bool SetWarehouseDeliveryToDB(ICollection<WarehouseAcceptance> aDeliveries)
        {
            // TODO: ?async? set WarehouseDelivery collection with only changed items to DB           
            return false;
        }


        public bool SetSuppliersToDB(ICollection<Supplier> aSuppliers)
        {
            // TODO: ?async? set Supplier collection with only changed items to DB
            return false;
        }

        public bool SetComponentSupplierProfileToDB(ComponentSupplierProfile aProfile)
        {
            // TODO: ?async? set ComponentSupplierProfile with only changed items to DB
            return false;
        }


        public bool GetComponentSupplierProfile()
        {
            // TODO: ?async? get ComponentSupplierProfile data from DB
            return false;
        }


        public bool GetComponentWarehouseDelivery()
        {
            // TODO: ?async? get WarehouseDelivery data from DB

            ICollection<WarehouseDelivery> collection = new Collection<WarehouseDelivery>();
            collection.Add(new WarehouseDelivery() { Id = 1, DeliveryDate = "12.12.2020", Quantity = 40, WhomWereIssued = "Иванов" });
            collection.Add(new WarehouseDelivery() { Id = 2, DeliveryDate = "22.12.2020", Quantity = 30, WhomWereIssued = "Петров" });

            WarehouseDeliveryFromDBEvent?.BeginInvoke(this, new TEventArgs<ICollection<WarehouseDelivery>>(collection), null, null);

            return true;
        }


        public bool GetComponentWarehouseAcceptance()
        {

            // TODO: ?async? get WarehouseAcceptance data from DB

            ICollection<WarehouseAcceptance> collection = new Collection<WarehouseAcceptance>();
            collection.Add(new WarehouseAcceptance() { Id = 1, AcceptanceDate = "11.12.2020", Quantity = 50 });
            collection.Add(new WarehouseAcceptance() { Id = 2, AcceptanceDate = "21.12.2020", Quantity = 40 });

            WarehouseAcceptanceFromDBEvent?.BeginInvoke(this, new TEventArgs<ICollection<WarehouseAcceptance>>(collection), null, null);

            return true;
        }


        public bool GetSuppliers()
        {
            // TODO: ?async? get WarehouseAcceptance data from DB

            ICollection<Supplier> collection = new Collection<Supplier>();
            collection.Add(new Supplier()
            {
                Id = 1,
                AcceptanceType = AcceptanceTypes.TCD,
                Delivery = new DeliveryInterval()
                {
                    DeliveryTimeMin = 2,
                    DeliveryTimeMax = 4
                },
                Name = "дядя вася",
                Note = "примечание",
                Packing = "О",
                Price = 1000.0f,
                TaxType = TaxTypes.Tax20,
                PriceWithTax = 1200.0f,
                Quantity = 50
            });

            collection.Add(new Supplier()
            {
                Id = 2,
                AcceptanceType = AcceptanceTypes.MA,
                Delivery = new DeliveryInterval()
                {
                    DeliveryTimeMin = 6,
                    DeliveryTimeMax = 8
                },
                Name = "рога и копыта",
                Note = "примечание",
                Packing = "Н",
                Price = 1200.0f,
                TaxType = TaxTypes.Tax20,
                PriceWithTax = 1440.0f,
                Quantity = 40
            });


            SupplierFromDBEvent?.BeginInvoke(this, new TEventArgs<ICollection<Supplier>>(collection), null, null);            

            return true;
        }
    }
}
