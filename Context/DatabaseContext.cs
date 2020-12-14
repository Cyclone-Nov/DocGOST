﻿using GostDOC.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GostDOC.Context
{
    public class DatabaseContext : DbContext
    {
        public DatabaseContext()
        {
            Database.EnsureCreated();
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Filename=Database.db");
        }

        public DbSet<ComponentSupplierProfile> Profiles { get; set; }

        public DbSet<ComponentEntry> Entries { get; set; }

        public DbSet<Supplier> Suppliers { get; set; }

        public DbSet<SupplierProperties> SupplierProperties { get; set; }

        public DbSet<WarehouseAcceptance> WarehouseAcceptancies { get; set; }

        public DbSet<WarehouseDelivery> WarehouseDeliveries { get; set; }

        public DbSet<DeliveryInterval> DeliveryIntervals { get; set; }
    }
}