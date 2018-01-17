﻿using System;
using System.Data.Entity;
using System.Data.SQLite;
using System.IO;
using System.Reflection;
using SQLite.CodeFirst;

namespace BatteryMonitoringSystem.Models
{
    public class SystemDbContext : DbContext
    {
        public SystemDbContext(string connectionString)
            : base(new SQLiteConnection() { ConnectionString = connectionString }, true)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentNullException("connectionStringName", "The name of a standard connection string in the config file must be supplied");

            AppDomain.CurrentDomain.SetData("DataDirectory", Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            Database.Connection.ConnectionString = connectionString;
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            Database.SetInitializer(new SqliteCreateDatabaseIfNotExists<SystemDbContext>(modelBuilder));
        }

        public DbSet<InformationSource> InformationSources { get; set; }
        public DbSet<Information> Informations { get; set; }
    }
}
