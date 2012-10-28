using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xbim.XbimExtensions
{
    public enum XbimDBAccess
    {
        /// <summary>
        /// Opens the database for read only transactions
        /// Data accessed is cached during the scope of the transaction
        /// Efficent where a subset of the database is retrieved and accessed frequently
        /// No duplicates of entities are retrieved
        /// </summary>
        Read,
        /// <summary>
        /// Opens the database for readonly but does not cache data that has been retrieved
        /// This improves memory use for large database-wide operations
        /// It can retrieve duplicate instances of objects
        /// </summary>
        ReadNoCache,
        /// <summary>
        /// Opens the database for read and write transactions, 
        /// Data accessed is cached during the scope of the transaction
        /// Efficent where a subset of the database is retrieved and accessed frequently
        /// No duplicates of entities are retrieved
        /// </summary>
        ReadWrite,
        /// <summary>
        /// Opens the database for read and write transactions
        /// This improves memory use for large database-wide operations
        /// It can retrieve duplicate instances of objects
        /// </summary>
        ReadWriteNoCache,
        /// <summary>
        /// Opens the database exclusively, prevents access from any other processes.
        /// </summary>
        Exclusive
    }
}
