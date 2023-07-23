﻿using Cerebro.Core.Models.Common;
using Cerebro.Core.Models.Entities.Base;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Cerebro.Core.Models.Entities.Clients.Applications
{
    public class ApplicationToken : BaseEntity
    {
        // Id is being used as API Key (Application Token Key)
        public Guid Id { get; set; }

        // foreign key
        public int ApplicationId { get; set; }

        public CryptographyTypes CryptographyType { get; set; }

        
        [JsonIgnore]
        public string? HashedSecret { get; set; }

        public DateTimeOffset ExpireDate { get; set; }
        public string? Description { get; set; }
        public DateTimeOffset IssuedDate { get; set; }
    }
}
