using System;
using System.Collections.Generic;

namespace Yintai.Hangzhou.Data.Models
{
    public partial class OrderTransactionEntity : Yintai.Architecture.Common.Models.BaseEntity
    {
        public int Id { get; set; }
        public string OrderNo { get; set; }
        public string PaymentCode { get; set; }
        public decimal Amount { get; set; }
        public System.DateTime CreateDate { get; set; }
        public string TransNo { get; set; }
        public bool IsSynced { get; set; }
        public DateTime? SyncDate { get; set; }

        #region Overrides of BaseEntity

        /// <summary>
        /// KeyMemberId
        /// </summary>
        public override object EntityId
        {       
                get { return Id; }
 
        }

        #endregion
    }
}
